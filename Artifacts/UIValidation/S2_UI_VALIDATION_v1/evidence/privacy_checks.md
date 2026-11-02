# Privacy checks

| Check | Result | Evidence |
|---|---|---|
| Mano rival boca abajo / no VisibleHand | PASS | MatchView projector; PlayMode RivalHand_NeverVisible |
| No nombres CardId rival ocultos | PASS | HandView only renders view.Viewer.VisibleHand |
| Orden mazo no expuesto | PASS | Normal MatchView has no deck order |
| Pass-device sin filtración | PASS | Capture 03_pass_device; hand hidden while overlay |
| GameOver sin revelar mano rival | PASS | Capture 13; HandView still viewer-only |
| MatchDebugHud off by default | PASS | Not on Battle; SampleScene enableDebugHud default false |
| Logs sin volcar manos privadas | PASS | Console during validation: only Bootstrap 120/120 log |

P0 fugas: **ninguna confirmada**.
