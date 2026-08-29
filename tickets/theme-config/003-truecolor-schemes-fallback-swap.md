---
title: Add TrueColor schemes + 16-color fallback theme with capability-gated swap
classification: Independent
blocked_by: [002-glyphset-config-accessor-di]
parent: docs/decisions/DECISIONS-CoAttribution-tui-modernization.md
---

## Goal

Define a modern neutral TrueColor palette that fills every `VisualRole`, plus a 16-color fallback theme, and switch to the fallback automatically when the terminal lacks truecolor - without regressing on capable terminals.

## What to build

1. In `src/CoAttribution.Cli/Resources/config.json`, add 24-bit `Schemes` filling `Base`/`Dialog`/`HotNormal`/`HotFocus`/`Active`/`ReadOnly`/`Disabled` using `#RRGGBB` colors (T007, D002). Add a `CoAttribution.Fallback` theme using 16-color names (T010).
2. Edit `ThemeConfigurationHelper.ApplyTheme` to detect terminal truecolor capability and `SwitchTheme` to the fallback when unsupported; keep `ApplyTheme` AoT-safe (no reflection) (T010, T007).

## Size

- **Files** - 2 (edit `config.json`, edit `ThemeConfigurationHelper.cs`)

## Recommended Workflow

### Step 1 - Add TrueColor schemes to config.json

Where: `src/CoAttribution.Cli/Resources/config.json`

- Add `#RRGGBB` color schemes for all 7 `VisualRole`s under the `CoAttribution` theme (note: current config uses `Base`/`Dialog` only; add the missing roles).

Verify: All 7 roles present with 24-bit hex colors.

### Step 2 - Add fallback theme

Where: `src/CoAttribution.Cli/Resources/config.json`

- Add a `CoAttribution.Fallback` theme using 16-color names for the same roles.

Verify: Fallback theme is well-formed and selectable by name.

### Step 3 - Implement capability-gated swap

Where: `src/CoAttribution.Cli/Tui/Composition/ThemeConfigurationHelper.cs`

- Detect truecolor support (terminal/driver capability) and call `SwitchTheme` to `CoAttribution.Fallback` when unsupported.
- Keep `ApplyTheme` reflection-free.

Verify: On a truecolor terminal the primary theme loads; on a non-truecolor terminal the fallback loads; AoT analyzer passes.

## Context pointers

##### Files

- `src/CoAttribution.Cli/Resources/config.json` - add TrueColor schemes and fallback theme.
- `src/CoAttribution.Cli/Tui/Composition/ThemeConfigurationHelper.cs` - capability-gated swap.

##### ADRs

- `docs/adr/0001-native-aot-constraint.md` - AoT-safe theme application.

##### Ledger records

- `DECISIONS-CoAttribution-tui-modernization.md#T007` - TrueColor schemes inline as `#RRGGBB`.
- `DECISIONS-CoAttribution-tui-modernization.md#T010` - capability-gated truecolor fallback swap.
- `DECISIONS-CoAttribution-tui-modernization.md#T002` - Terminal.Gui v2 APIs assumed.
- `DECISIONS-CoAttribution-tui-modernization.md#D002` - TrueColor neutral palette filling all `VisualRole`s.

## Acceptance criteria

- [ ] `config.json` `CoAttribution` theme defines `Base`/`Dialog`/`HotNormal`/`HotFocus`/`Active`/`ReadOnly`/`Disabled` schemes with `#RRGGBB` (T007, D002)
- [ ] A `CoAttribution.Fallback` 16-color theme exists (T010)
- [ ] At startup, when truecolor is unsupported, the fallback theme is selected automatically (T010)
- [ ] Capable terminals keep the TrueColor theme; no regression (D002)
- [ ] `ApplyTheme` remains AoT-safe (no reflection) (T010, ADR 0001)

## Dependencies

**Blocked by** - 002-glyphset-config-accessor-di (shared `config.json` edit)
