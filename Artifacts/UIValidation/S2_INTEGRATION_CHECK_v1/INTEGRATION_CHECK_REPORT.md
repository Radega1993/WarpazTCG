# S2 Integration Check v1 — Paso 1

- Date: 2026-11-02
- Unity: 6000.3.20f1
- Scope: compile + EditMode/PlayMode + live captures (hand / board / player swap)
- Domain/Application: untouched

## Tests

| Suite | Result |
| --- | --- |
| EditMode | **167/167 PASS** |
| PlayMode | **13/13 PASS** |
| Console errors during live capture | **0** |

## Live metrics

| Capture | handActive | handIll | oppActive | oppIll | Notes |
| --- | ---: | ---: | ---: | ---: | --- |
| 02_main_hand_board | 6 | 6 | 6 | 0 | Art on local hand; opponent face-down |
| 03_pass_device | 0 | 0 | 6 | 0 | Local hand hidden; Confirm visible |
| 04_after_player_swap | 7 | 7 | 6 | 0 | Next viewer hand illustrated; privacy OK |

Board occupied art not exercised (no characters in play this seed); empty slots remain grey placeholders (expected).

## Captures

- `screenshots/01_hand_mulligan.png` — early Battle (pass overlay timing)
- `screenshots/02_main_hand_board.png` — Main, art visible in hand
- `screenshots/03_pass_device.png` — privacy overlay
- `screenshots/04_after_player_swap.png` — post-Confirm hand for next player

## Verdict

**PASS** — no regressions; illustrations visible on face-up local hand; opponent hand and pass-device keep privacy.

## Known open items (not fixed here)

1. Intercept legal path blocked by P-HeroReach vs P-Interceptors (ADR 0004) — needs design decision before code change.
2. Card detail / effect text not consultable while art fills chrome; effects not executable (`EffectDefinition` empty; PlayCharacter-only).
