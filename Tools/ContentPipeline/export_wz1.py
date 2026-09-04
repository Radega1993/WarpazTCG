#!/usr/bin/env python3
"""Export Base Set WZ1 (Excel editorial source) to versioned UTF-8 JSON.

Runtime never reads XLSX. Re-run from the repository root:

    python3 Tools/ContentPipeline/export_wz1.py

Requires CPython 3.10+ (stdlib only). See requirements.txt.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path

NS = {"m": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
REL_NS = "{http://schemas.openxmlformats.org/officeDocument/2006/relationships}"

SHEET_CARTAS = "Cartas"
EXPECTED_HEADERS = [
    "ID",
    "Set",
    "Nº",
    "Nombre",
    "Tipo",
    "Subtipo",
    "Raza",
    "Clase",
    "Coste PA",
    "Poder",
    "Respuesta",
    "Rareza",
    "Única",
    "Posición",
    "Palabras clave",
    "Efecto",
    "Ambientación",
    "Arquetipo",
    "Canon",
    "Complejidad",
    "Notas de equilibrio",
    "Control ID",
    "Control stats",
]
CARD_KEYS = [
    "cardId",
    "setId",
    "number",
    "name",
    "type",
    "subtype",
    "race",
    "class",
    "costPA",
    "power",
    "response",
    "rarity",
    "unique",
    "position",
    "keywords",
    "effectText",
    "flavor",
    "archetype",
    "canon",
    "complexity",
    "balanceNotes",
    "effects",
]
EXPECTED_TYPE_COUNTS = {
    "Personaje": 60,
    "Acción": 20,
    "Equipo": 13,
    "Héroe": 11,
    "Reacción": 10,
    "Clase": 6,
}
IDENTITY_TYPES = ("Héroe", "Clase")
CONTENT_VERSION = "WZ1-v0.1"
SCHEMA_VERSION = "0.1"
SET_ID = "WZ1"
COL_REF = re.compile(r"^([A-Z]+)")


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def col_index(cell_ref: str) -> int:
    letters = COL_REF.match(cell_ref).group(1)
    n = 0
    for ch in letters:
        n = n * 26 + (ord(ch) - 64)
    return n - 1


def load_shared_strings(zf: zipfile.ZipFile) -> list[str]:
    name = "xl/sharedStrings.xml"
    if name not in zf.namelist():
        return []
    root = ET.fromstring(zf.read(name))
    out: list[str] = []
    for si in root.findall("m:si", NS):
        texts = [t.text or "" for t in si.iter("{http://schemas.openxmlformats.org/spreadsheetml/2006/main}t")]
        out.append("".join(texts))
    return out


def cell_value(cell: ET.Element, strings: list[str]) -> str | None:
    kind = cell.attrib.get("t")
    inline = cell.find("m:is", NS)
    value = cell.find("m:v", NS)
    if kind == "s" and value is not None and value.text is not None:
        return strings[int(value.text)]
    if kind == "inlineStr" and inline is not None:
        return "".join(
            (t.text or "")
            for t in inline.iter("{http://schemas.openxmlformats.org/spreadsheetml/2006/main}t")
        )
    if value is None or value.text is None:
        return None
    return value.text


def load_sheet_rows(zf: zipfile.ZipFile, worksheet_path: str, strings: list[str]) -> dict[int, list]:
    root = ET.fromstring(zf.read(worksheet_path))
    rows: dict[int, list] = {}
    for row in root.findall("m:sheetData/m:row", NS):
        r = int(row.attrib["r"])
        cells: dict[int, str | None] = {}
        max_c = -1
        for cell in row.findall("m:c", NS):
            idx = col_index(cell.attrib["r"])
            cells[idx] = cell_value(cell, strings)
            max_c = max(max_c, idx)
        if cells:
            rows[r] = [cells.get(i) for i in range(max_c + 1)]
    return rows


def worksheet_target(zf: zipfile.ZipFile, sheet_name: str) -> str:
    wb = ET.fromstring(zf.read("xl/workbook.xml"))
    rels = ET.fromstring(zf.read("xl/_rels/workbook.xml.rels"))
    rid_to_target = {rel.attrib["Id"]: rel.attrib["Target"].lstrip("/") for rel in rels}
    for sheet in wb.findall("m:sheets/m:sheet", NS):
        if sheet.attrib["name"] == sheet_name:
            rid = sheet.attrib[REL_NS + "id"]
            target = rid_to_target[rid]
            if not target.startswith("xl/"):
                target = "xl/" + target
            return target
    raise SystemExit(f"Sheet {sheet_name!r} was not found in the workbook.")


def empty(value: str | None) -> bool:
    return value is None or str(value).strip() == ""


def text_or_none(value: str | None) -> str | None:
    if empty(value):
        return None
    return str(value)


def parse_int(value: str | None, field: str, card_id: str) -> int:
    parsed = parse_int_or_none(value)
    if parsed is None:
        raise SystemExit(f"{card_id}: {field} must be an integer, got {value!r}.")
    return parsed


def parse_int_or_none(value: str | None) -> int | None:
    if empty(value):
        return None
    raw = str(value).strip()
    if re.fullmatch(r"-?\d+", raw):
        return int(raw)
    if re.fullmatch(r"-?\d+\.0+", raw):
        return int(float(raw))
    raise SystemExit(f"Not an integer: {value!r}")


def parse_unique(value: str | None, card_id: str) -> bool:
    if value == "Sí":
        return True
    if value == "No":
        return False
    raise SystemExit(f"{card_id}: Única must be 'Sí' or 'No', got {value!r}.")


def split_keywords(value: str | None) -> list[str] | None:
    if empty(value):
        return None
    parts = [part.strip() for part in str(value).split(";")]
    parts = [part for part in parts if part]
    return parts or None


def ordered_card(card: dict) -> dict:
    return {key: card[key] for key in CARD_KEYS}


def read_cartas(xlsx_path: Path) -> list[dict]:
    with zipfile.ZipFile(xlsx_path) as zf:
        strings = load_shared_strings(zf)
        target = worksheet_target(zf, SHEET_CARTAS)
        rows = load_sheet_rows(zf, target, strings)

    header_row_index = None
    for index, values in rows.items():
        padded = values + [None] * (len(EXPECTED_HEADERS) - len(values))
        if padded[: len(EXPECTED_HEADERS)] == EXPECTED_HEADERS:
            header_row_index = index
            break
    if header_row_index is None:
        raise SystemExit("Cartas header row with 23 expected columns was not found.")

    cards: list[dict] = []
    for index in sorted(r for r in rows if r > header_row_index):
        raw = rows[index][:]
        while len(raw) < 23:
            raw.append(None)
        raw = raw[:23]
        if all(empty(cell) for cell in raw):
            continue
        card_id = text_or_none(raw[0])
        if card_id is None:
            raise SystemExit(f"Row {index}: missing ID.")
        keywords = split_keywords(raw[14])
        card = {
            "cardId": card_id,
            "setId": text_or_none(raw[1]),
            "number": parse_int(raw[2], "Nº", card_id),
            "name": text_or_none(raw[3]),
            "type": text_or_none(raw[4]),
            "subtype": text_or_none(raw[5]),
            "race": text_or_none(raw[6]),
            "class": text_or_none(raw[7]),
            "costPA": parse_int_or_none(raw[8]),
            "power": parse_int_or_none(raw[9]),
            "response": parse_int_or_none(raw[10]),
            "rarity": text_or_none(raw[11]),
            "unique": parse_unique(raw[12], card_id),
            "position": text_or_none(raw[13]),
            "keywords": keywords,
            "effectText": text_or_none(raw[15]),
            "flavor": text_or_none(raw[16]),
            "archetype": text_or_none(raw[17]),
            "canon": text_or_none(raw[18]),
            "complexity": parse_int_or_none(raw[19]),
            "balanceNotes": text_or_none(raw[20]),
            "effects": [],
        }
        cards.append(ordered_card(card))

    cards.sort(key=lambda card: (card["number"], card["cardId"]))
    return cards


def validate(cards: list[dict]) -> list[str]:
    errors: list[str] = []
    if len(cards) != 120:
        errors.append(f"Expected 120 cards, found {len(cards)}.")

    ids = [card["cardId"] for card in cards]
    if len(ids) != len(set(ids)):
        dupes = sorted({card_id for card_id in ids if ids.count(card_id) > 1})
        errors.append("Duplicate IDs: " + ", ".join(dupes))

    expected_ids = [f"WZ1-{n:03d}" for n in range(1, 121)]
    missing = [card_id for card_id in expected_ids if card_id not in ids]
    extra = [card_id for card_id in ids if card_id not in expected_ids]
    if missing:
        errors.append("Missing IDs: " + ", ".join(missing))
    if extra:
        errors.append("Unexpected IDs: " + ", ".join(extra))

    for card in cards:
        expected = f"WZ1-{card['number']:03d}"
        if card["cardId"] != expected:
            errors.append(
                f"{card['cardId']}: ID does not follow number {card['number']} (expected {expected})."
            )
        if card["cardId"] == card["name"]:
            errors.append(f"{card['cardId']}: cardId must not be the display name.")
        if card["setId"] != SET_ID:
            errors.append(f"{card['cardId']}: setId {card['setId']!r} is not {SET_ID}.")
        if card["effects"] != []:
            errors.append(f"{card['cardId']}: effects must be empty in this phase.")
        if card["type"] not in EXPECTED_TYPE_COUNTS:
            errors.append(f"{card['cardId']}: unknown type {card['type']!r}.")

        identity = card["type"] in IDENTITY_TYPES
        if not identity:
            if card["costPA"] is None:
                errors.append(f"{card['cardId']}: non-identity card is missing Coste PA.")
            if card["response"] is None:
                errors.append(f"{card['cardId']}: non-identity card is missing Respuesta.")
        if card["type"] == "Personaje" and card["power"] is None:
            errors.append(f"{card['cardId']}: Personaje is missing Poder.")

    counts = {name: 0 for name in EXPECTED_TYPE_COUNTS}
    for card in cards:
        if card["type"] in counts:
            counts[card["type"]] += 1
    for name, expected in EXPECTED_TYPE_COUNTS.items():
        if counts[name] != expected:
            errors.append(f"Type {name}: expected {expected}, found {counts[name]}.")

    identities = sum(1 for card in cards if card["type"] in IDENTITY_TYPES)
    heroes = counts["Héroe"]
    classes = counts["Clase"]
    deck = len(cards) - identities
    if identities != 17:
        errors.append(f"Identities: expected 17, found {identities}.")
    if heroes != 11 or classes != 6:
        errors.append(f"Identities split: expected 11 Héroes + 6 Clases, found {heroes} + {classes}.")
    if deck != 103:
        errors.append(f"Main-deck cards: expected 103, found {deck}.")

    return errors


def dump_json(payload: dict) -> bytes:
    text = json.dumps(payload, ensure_ascii=False, indent=2) + "\n"
    return text.encode("utf-8")


def sha256_hex(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def build_catalog_document(cards: list[dict]) -> dict:
    return {
        "schemaVersion": SCHEMA_VERSION,
        "contentVersion": CONTENT_VERSION,
        "setId": SET_ID,
        "sourceWorkbook": "Docs/Source/warpaz_tcg_base_set_wz1_v0_1.xlsx",
        "sheet": SHEET_CARTAS,
        "cardCount": len(cards),
        "cards": cards,
    }


def build_manifest(cards: list[dict], catalog_sha256: str) -> dict:
    counts = {name: 0 for name in EXPECTED_TYPE_COUNTS}
    for card in cards:
        counts[card["type"]] += 1
    return {
        "schemaVersion": SCHEMA_VERSION,
        "contentVersion": CONTENT_VERSION,
        "setId": SET_ID,
        "expectedCardCount": 120,
        "firstCardId": "WZ1-001",
        "lastCardId": "WZ1-120",
        "identityCount": 17,
        "heroCount": 11,
        "classCount": 6,
        "mainDeckCardCount": 103,
        "catalogFile": "cards_wz1.v0.1.json",
        "sourceWorkbook": "Docs/Source/warpaz_tcg_base_set_wz1_v0_1.xlsx",
        "sheet": SHEET_CARTAS,
        "catalogSha256": catalog_sha256,
        "typeCounts": counts,
    }


def export(root: Path) -> tuple[str, list[str]]:
    xlsx = root / "Docs" / "Source" / "warpaz_tcg_base_set_wz1_v0_1.xlsx"
    if not xlsx.is_file():
        raise SystemExit(f"Missing workbook: {xlsx}")

    cards = read_cartas(xlsx)
    errors = validate(cards)
    out_dir = root / "Assets" / "StreamingAssets" / "Data"
    out_dir.mkdir(parents=True, exist_ok=True)
    catalog_path = out_dir / "cards_wz1.v0.1.json"
    manifest_path = out_dir / "content_manifest.v0.1.json"

    catalog_bytes = dump_json(build_catalog_document(cards))
    digest = sha256_hex(catalog_bytes)
    manifest_bytes = dump_json(build_manifest(cards, digest))
    catalog_path.write_bytes(catalog_bytes)
    manifest_path.write_bytes(manifest_bytes)
    return digest, errors


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description="Export WZ1 Cartas sheet to versioned JSON.")
    parser.add_argument("--root", type=Path, default=None, help="Repository root (default: inferred).")
    args = parser.parse_args(argv)
    root = args.root.resolve() if args.root else repo_root()
    digest, errors = export(root)
    print(f"Wrote Assets/StreamingAssets/Data/cards_wz1.v0.1.json")
    print(f"Wrote Assets/StreamingAssets/Data/content_manifest.v0.1.json")
    print(f"catalogSha256={digest}")
    if errors:
        print("VALIDATION FAILED:", file=sys.stderr)
        for error in errors:
            print(" - " + error, file=sys.stderr)
        return 1
    print("Validation passed: 120 cards, WZ1-001…WZ1-120.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
