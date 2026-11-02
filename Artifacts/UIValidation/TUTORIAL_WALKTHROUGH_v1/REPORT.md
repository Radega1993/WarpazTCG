# Tutorial walkthrough validation

Date: 2026-11-02  
Harness: Linux player `-tutorialCapture`  
Shots: `Artifacts/UIValidation/TUTORIAL_WALKTHROUGH_v1/screenshots/`

## Result

**PASS** (flow completes; board cards + card backs visible after board chrome fix).

| Shot | Step | Check |
|------|------|--------|
| 00_welcome | Coach intro | Overlay + hand OK |
| 01_mulligan | Conservar | Mulligan panel OK |
| 02_pa_intro | PA concept | HUD OK |
| 03_play_iniciado_prompt | Play prompt | Coach OK |
| 04_after_play_board_visible | After WZ1-018 | **Board shows Iniciado** |
| 05_tutor_turn_observe | Wait tutor | Card backs on opp hand + mazos |
| 06_tutor_done | Tutor finished | **Tutor Bruto + learner Iniciado on board** |
| 07–11 | Reorg / attack / finish | Flow advances to finish |

## Fixes validated

- Card backs on opponent hand + Mazo/Desc.
- Board FaceUpCard no longer covered by root grey chrome / black frame.
- Mulligan gate waits for MulliganP1 (tutor) before advancing.

## Cartas browser

Main menu **Cartas** opens `CardsBrowserOverlay` with full WZ1 catalog grid.
