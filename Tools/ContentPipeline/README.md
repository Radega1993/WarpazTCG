# Tools / ContentPipeline

Offline exporter: Excel WZ1 → UTF-8 JSON. Domain never opens XLSX.

## Dependencies (development only)

Pinned in `requirements.txt`: **CPython 3.10+**, standard library only
(`json`, `hashlib`, `zipfile`, `xml.etree`). No pip packages.

## Regenerate WZ1 JSON

From the repository root, after editing the editorial workbook
`Docs/Source/warpaz_tcg_base_set_wz1_v0_1.xlsx` (do not edit that file from
tooling):

```bash
python3 Tools/ContentPipeline/export_wz1.py
```

Writes:

- `Assets/StreamingAssets/Data/cards_wz1.v0.1.json`
- `Assets/StreamingAssets/Data/content_manifest.v0.1.json`

The manifest `catalogSha256` is SHA-256 (lowercase hex) of the exact UTF-8
bytes of the catalog file. A second run must produce the same hash.

`contentVersion` is `WZ1-v0.1`. `effects` is always `[]` in this phase.
Control ID / Control stats stay in Excel only.
