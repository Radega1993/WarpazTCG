# Card visual checks (grey face)

Authority: `CharacterSlotView` (board) + `CardPlaceholderView` (hand). No card face art sprites assigned.

| Comprobación | Resultado | Evidencia |
|---|---|---|
| Fondo gris | PASS | TMP on grey UI; Frame is slot chrome only; no face Image art |
| Sin ilustración | PASS | No portrait/landscape Sprite on card content |
| Sin silueta | PASS | Hierarchy: Frame/CardContainer/SlotLabel/overlays only |
| Texto legible | PASS/PARTIAL | TMP SlotLabel / Hand Text; see captures 05–09 |
| Proporción estable | PASS | RectTransform sizeDelta layout in BoardSlotsView/HandView |
| Selección visible | PARTIAL | Board: SelectionOverlay gold; Hand: no dedicated overlay (click only) |
| Agotamiento visible | PASS (code) | StatusOverlay + tint/rotation in CharacterSlotView.SetExhausted |

Cualquier ilustración/silueta en carta boca arriba: **no observada**.
