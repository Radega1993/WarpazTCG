# PRE-SPRINT-2 — Battle Board UI Integration

- Date: 2026-09-04
- Scope: **Presentation only** (not Sprint 2 rules)
- Editor: Unity **6000.3.20f1**

## Objetivo

Sustituir la mesa gris provisional de `Battle` por la interfaz visual WARPAZ (fondo, marcos de slot, paneles 9-slice, iconos PA/Resolución) sin modificar Domain, Application, reglas, comandos, eventos ni catálogo.

## Assets utilizados

| Archivo | Ubicación definitiva |
| --- | --- |
| `warpaz_battleboard_background.png` | `Assets/Warpaz/Presentation/Art/UI/Battle/` |
| `warpaz_character_slot_frame.png` | idem |
| `warpaz_panel_frame_9slice.png` | idem |
| `warpaz_pa_icon.png` | idem |
| `warpaz_resolution_icon.png` | idem |

Origen: `Assets/Images/` (vacío y eliminado tras el move). Sin duplicados.

## Configuración de importación

Todos: Sprite (2D and UI), **Single**, sRGB on, mips off, Clamp, Bilinear, Compressed HQ.

| Asset | Alpha Is Transparency | Max Size | spriteBorder |
| --- | --- | --- | --- |
| background | off | 2048 | 0 |
| slot frame | on | 1024 | 0 |
| PA / Resolution icons | on | 512 | 0 |
| panel 9-slice | on | 2048 | L180 B150 R180 T150 |

## Prefabs / scripts

- Prefab: `Assets/Warpaz/Presentation/Prefabs/Battle/CharacterSlotView.prefab`
- Scripts: `CharacterSlotView`, `BattleBoardArtBinder`; updates to `BoardSlotsView`, `PlayerAreaView`, `HandView`, `MatchStatusView`, `BattlePresenter` (selection/legal overlays only)

## Escena

- `Assets/Warpaz/Presentation/Scenes/Battle.unity`
- Canvas Scaler unchanged: Scale With Screen Size, 1920×1080, Match 0.5
- `Background`: sprite de mesa, `raycastTarget=false`, AspectRatioFitter EnvelopeParent
- `BattleCompositionRoot` + `BattleBoardArtBinder` con sprites asignados
- Slots `BoardSlot0–7` conservados (PlayMode Find contracts)

## Jerarquía visual resultante

```
Canvas
  Background                    (mesa, no raycast)
  BattleCompositionRoot         (+ BattleBoardArtBinder)
    PlayerAreaView              (paneles 9-slice + iconos PA/Res)
    BoardSlotsView              (CharacterSlotView × 8)
    HandView
    MatchStatusView
    EventLogView
    MulliganPanel               (KeepHandButton / ReplaceHandButton)
    PassDeviceOverlay           (ConfirmButton)
    BattleControlsView          (EndTurnButton)
EventSystem
```

## Responsive

Anchors en HUD/mano/status; fondo cover 16:9; verificaciones 1920×1080, 1600×900, 1280×720 (+ PlayMode 1366×768).

## Estados visuales

Unity-only: normal / Exhausted (tint + leve rotación) / Selected (dorado) / LegalTarget (violeta). Sin PNG nuevos de estado. Sin ilustraciones de cartas.

## Privacidad

UI sigue `MatchView` / `session.Project`. Mano rival no se renderiza en `HandView` cuando `VisibleHand` está vacío. No enlace a `GameState` crudo.

## Capturas

`Assets/Warpaz/Presentation/Captures/PreSprint2/`:

- `battle-empty-sceneview.png`
- `mesa-vacia-gameview.png` / `mesa-vacia-1920x1080.png` (+ intentos 1600/1280)
- Estados Mulligan / Attack / GameOver: cubiertos por PlayMode existente; capturas play-mode adicionales documentadas como deuda si GameView editor no tiene sesión Bootstrap.

## Tests ejecutados

| Suite | Resultado |
| --- | --- |
| EditMode | **153/153** PASS (incluye 3 tests PRE-SPRINT-2 UI) |
| PlayMode | **8/8** PASS (privacidad mano rival + Battle flow) |

0 FAIL. 0 errores de compilación al cierre. PlayMode sin Missing Scripts en el flujo Bootstrap→Battle.

## Confirmación Domain / Application

**Esta tarea no modificó Domain ni Application.** Solo Presentation, Art, escena Battle, tests de presentación y esta documentación.

## Deuda técnica

- Layout Awake aún mezcla anchors con offsets; refinar con LayoutGroups.
- Capturas de estados de combate/mulligan en Game View requieren Bootstrap play session (PlayMode las valida).
- `MatchDebugHud` permanece fuera de Battle (SampleScene), desactivado por defecto.
- WIP previo de Sprint 2 combate en el working tree es ajeno a esta integración y no fue revertido ni ampliado aquí.

## Veredicto

`READY_FOR_SPRINT_2` (integración visual previa lista; no abre implementación de reglas Sprint 2 en este documento).
