# WARPAZ TCG — S2 UI Validation 01

## 1. Veredicto

**`UI_VALIDATION_FAIL_P1`**

Sprint 2 permanece abierto. Esta validación no declara Sprint 2 complete.

## 2. Alcance

Validación visual/funcional de Battle UI post PRE-SPRINT-2 art integration y S2 combate WIP en working tree. Sin correcciones. Sin modificar Domain/Application/escena/prefabs de producción.

## 3. Estado de Git

Ver `evidence/git_status.txt`. Working tree sucio (S2 Domain/Application WIP + Presentation UI) **previo** a esta fase. Esta fase solo añadió:

- `Artifacts/UIValidation/S2_UI_VALIDATION_v1/`
- `Assets/Editor/UIValidation/`
- `Assets/Tests/*/UIValidation/`

Commit HEAD: `490b38ca8aca6e0e916ee4200d52bb037278dd3b`

## 4. Estado de compilación

0 errores al cierre del drive. Missing Scripts en Battle: **0**. Ver `logs/compilation.log`.

## 5. Resultados EditMode

| Metric | Value |
| --- | --- |
| Total | 153 |
| Passed | 153 |
| Failed | 0 |
| Skipped | 0 |
| Duration | ~38.55s |

XML: `test-results/editmode-results.xml`

## 6. Resultados PlayMode

| Metric | Value |
| --- | --- |
| Total | 9 |
| Passed | 9 |
| Failed | 0 |
| Skipped | 0 |
| Duration | ~21.72s |

Incluye `UiValidationBlockedStatesTests`. XML: `test-results/playmode-results.xml`

## 7. Flujo funcional probado

| Acción | Control / API | Aceptado | Visual |
| --- | --- | --- | --- |
| Boot Bootstrap→MainMenu→Battle | SimulatePlay | Sí | Sí |
| Mulligan Keep | KeepHandButton | Sí | 01 |
| Pass-device | EndTurnButton → overlay | Sí | 03 |
| Play Character | Session.Play | Sí | 05/responsive |
| Hand click | HandCard Button | Sí | 06 PARTIAL highlight |
| ReorganizeMove | Session.ReorganizeMove | Sí | 08 PARTIAL |
| Attack Character | Session.Attack | Sí | 09 → 12 |
| AttackHero → GameOver | Session.AttackHero ×5 | Sí | 13 |
| Intercept | — | BLOCKED | 10/11 |
| Per-card mulligan | — | BLOCKED | 02 |
| MatchDebugHud on Battle | — | BLOCKED | 14 |

## 8. Validación por resolución

| Res | Capturas | Notas |
| --- | --- | --- |
| 1920x1080 | 01,03–09,12–13 PNG + BLOCKED 02,10,11,14 | Screen set via SetCustomResolution |
| 1600x900 | 01,02 PNG; 03 BLOCKED | Screen.width confirmed 1600x900 during layout dump |
| 1280x720 | 01,02 PNG; 03 BLOCKED | SetCustomResolution |

## 9. Validación de cartas grises

Ver `evidence/card_visual_checks.md` — sin ilustraciones/siluetas confirmadas. Selección de mano PARTIAL.

## 10. Validación de privacidad

Ver `evidence/privacy_checks.md` — **0 P0**.

## 11. Problemas encontrados

| ID | Severidad | Estado/fase | Resolución | Descripción | Captura | Causa probable | Corrección propuesta |
|---|---|---|---|---|---|---|---|
| UI-P1-001 | P1 | Battle layout | all | Bajo `Canvas/Background` existe jerarquía legacy `Panel/Title/Hint/BackButton` no propia de mesa de combate | hierarchy + 04/05 | **confirmada** (auditoría jerarquía) | Quitar o desactivar stub bajo Background (Presentation-only) |
| UI-P2-001 | P2 | Main hand select | 1920 | `CardPlaceholderView` no tiene overlay de selección; selección poco evidente | 06 | **confirmada** (código) | Overlay/tint en mano sin tocar Domain |
| UI-P2-002 | P2 | Captures | 1920 | Varias capturas secuenciales tienen tamaño idéntico (04=05; 06=07=08) → posible frame async repetido | 04–08 | **confirmada** (bytes de archivo) | Re-capturar con sync/wait (solo harness) |
| UI-P2-003 | P2 | Legal targets | 1920 | Resaltado de objetivos legales tras selección de mano no claramente diferenciado en captura | 07 | **hipótesis** | Verificar overlays vacíos + legal actions |
| UI-P3-001 | P3 | Layout | all | Mezcla Awake offsets + anchors; deuda documentada PRE-SPRINT-2 | layout_measurements.json | **confirmada** | LayoutGroups futuros |

## 12. Evidencias por problema

- UI-P1-001: `evidence/battle_hierarchy.txt`
- UI-P2-001: `CardPlaceholderView.cs` vs `CharacterSlotView` SelectionOverlay
- UI-P2-002: `ls -l screenshots/1920x1080`
- UI-P2-003: capturas 06 vs 07 mismos bytes
- UI-P3-001: `evidence/layout_measurements.json`

## 13. Estados no alcanzables

- 02 mulligan selected cards (producto Keep/Replace only)
- 10/11 intercept (+ responsive 03) — P-HeroReach
- 14 MatchDebugHud on Battle

## 14. Deuda técnica observada

- Session API EndTurn no dispara pass-device Presenter (solo botón UI)
- WIP Domain/Application S2 no versionado limpio
- Capturas async Game View frágiles

## 15. Recomendación para el siguiente paso

1. Corregir **UI-P1-001** (limpiar stub bajo Background) — Presentation only.
2. Re-capturar estados 04–08 con harness síncrono.
3. Mejorar highlight de mano (UI-P2-001).
4. No cerrar Sprint 2 hasta playtest visual + intercept policy decision si se requiere UI de intercepción.
