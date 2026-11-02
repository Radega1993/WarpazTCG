# WARPAZ S2 UI Validation v1

- **Fecha:** 2026-09-28T18:58:44+02:00
- **Commit:** 490b38ca8aca6e0e916ee4200d52bb037278dd3b
- **Unity:** 6000.3.20f1
- **Rule version:** 0.1.0 (RuleConfig.AlphaRuleVersion)
- **Content:** WZ1 / Match fixture Bootstrap fallback (seed=1 en BattleCompositionRoot)
- **Seed observado:** 1
- **Informe:** `UI_VALIDATION_REPORT.md`
- **Veredicto:** UI_VALIDATION_FAIL_P1
- **Defectos:** P0=0, P1=1, P2=3, P3=1

## Cómo revisar

1. Abrir `00_CONTACT_SHEET.png`
2. Revisar `screenshots/1920x1080/` en orden 01→13
3. Leer `*_BLOCKED.md` donde no hay PNG
4. Leer informe principal
5. Consultar `evidence/` y `test-results/`

## Orden recomendado

01 mulligan → 03 pass-device → 04/05 main → 09 attack → 12 KO → 13 GameOver → responsive 1600/1280 → BLOCKED docs.
