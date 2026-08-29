---
title: Add Glyphs config + GlyphSet record accessor + DI singleton
classification: Independent
blocked_by: []
parent: docs/decisions/DECISIONS-CoAttribution-tui-modernization.md
---

## Goal

Introduce AoT-safe Unicode glyph sourcing so the TUI can render modern indicators (selection check, arrow, warning, key glyphs) without reflection or a Nerdfont dependency.

## What to build

1. Add a `Glyphs` object to `src/CoAttribution.Cli/Resources/config.json` with keys `Check`, `Arrow`, `Warning`, `KeyEnter`, `KeyEsc`, `KeyTab`, `KeyCtrlEnter` using widely-supported Unicode (D004).
2. New file `src/CoAttribution.Cli/Tui/Composition/GlyphSet.cs` - a `public sealed record GlyphSet(...)` parsed once from the config `Glyphs` section via MEC (Microsoft.Extensions.Configuration) and exposed as a DI singleton. No runtime reflection (T006, T016). Lives in `CoAttribution.Cli.Tui` (T015).
3. Register `GlyphSet` in `TuiCompositionRoot` before `Application.Create()` (T016, T003).

## Size

- **Files** - 3 (edit `config.json`, new `GlyphSet.cs`, edit `TuiCompositionRoot.cs`)

## Recommended Workflow

### Step 1 - Add Glyphs section to config.json

Where: `src/CoAttribution.Cli/Resources/config.json`

- Add a `Glyphs` object with the 7 required keys using plain Unicode (no Nerdfont).

Verify: JSON is valid; the 7 keys are present.

### Step 2 - Create GlyphSet record

Where: `src/CoAttribution.Cli/Tui/Composition/GlyphSet.cs`

- Define `public sealed record GlyphSet(string Check, string Arrow, string Warning, string KeyEnter, string KeyEsc, string KeyTab, string KeyCtrlEnter)`.
- Parse once from the MEC `Glyphs` section via `IConfiguration`/bind, with no reflection.

Verify: Compiles; NativeAOT/AoT analyzer passes with no trimming warnings.

### Step 3 - Register singleton

Where: `src/CoAttribution.Cli/Tui/Composition/TuiCompositionRoot.cs`

- Register `GlyphSet` as a singleton in the DI container before `Application.Create()`.

Verify: `GlyphSet` resolves from `IServiceProvider` in `LaunchAsync`.

## Context pointers

##### Files

- `src/CoAttribution.Cli/Resources/config.json` - add the `Glyphs` block.
- `src/CoAttribution.Cli/Tui/Composition/GlyphSet.cs` - new record accessor.
- `src/CoAttribution.Cli/Tui/Composition/TuiCompositionRoot.cs` - DI registration site.

##### ADRs

- `docs/adr/0001-native-aot-constraint.md` - no reflection; AoT-safe config binding.

##### Ledger records

- `DECISIONS-CoAttribution-tui-modernization.md#T008` - `Glyphs.*` config block + record accessor.
- `DECISIONS-CoAttribution-tui-modernization.md#T006` - AoT-safe config sourcing, no reflection.
- `DECISIONS-CoAttribution-tui-modernization.md#T016` - `GlyphSet` record loaded from config.json.
- `DECISIONS-CoAttribution-tui-modernization.md#T015` - presentation types stay in `Cli.Tui`.
- `DECISIONS-CoAttribution-tui-modernization.md#D004` - Unicode-only glyphs, no Nerdfont.

## Acceptance criteria

- [ ] `config.json` has a `Glyphs` section with the 7 required keys (D004, T008)
- [ ] `GlyphSet` is a sealed record parsed once from config with no runtime reflection (T006, T016)
- [ ] `GlyphSet` is registered as a DI singleton and resolvable in `TuiCompositionRoot` (T016, T003)
- [ ] Glyphs use only widely-supported Unicode; no Nerdfont dependency (D004)
- [ ] New types live under `CoAttribution.Cli.Tui`; Lib stays Terminal.Gui-free (T015)

## Dependencies

**Blocked by** - None - can start immediately
