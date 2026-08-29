---
title: Status-bar split glyph/text cells via GlyphSet
classification: Independent
blocked_by: [002-glyphset-config-accessor-di, 004-feedbacktoast-title-removal]
parent: docs/decisions/DECISIONS-CoAttribution-tui-modernization.md
---

## Goal

Render status-bar shortcuts with a dedicated glyph cell and a text cell for discoverability, while keeping the bindings display-only (`Key.Empty`) so real key behavior is unchanged.

## What to build

1. Extend `IStatusBarProvider.StatusBarKeyBinding` to carry a `Glyph` (string from `GlyphSet`) plus `Label` (T012, T008).
2. Edit `StatusBarComposer.Build` to render each `Shortcut` with a glyph cell and a text cell using `GlyphSet`; keep `Key = Key.Empty` and `BindKeyToApplication = false` (T012, D005, T005).
3. Update `GetKeyBindings` on `MainWindow`, `CommitFormView`, and `AuthorSelectionView` to return entries with glyph + label from `GlyphSet` (T012, T008).

## Size

- **Files** - 5 (edit `IStatusBarProvider.cs`, `StatusBarComposer.cs`, `MainWindow.cs`, `CommitFormView.cs`, `AuthorSelectionView.cs`)

## Recommended Workflow

### Step 1 - Extend StatusBarKeyBinding

Where: `src/CoAttribution.Cli/Tui/Abstractions/IStatusBarProvider.cs`

- Add a `Glyph` field to the `StatusBarKeyBinding` record struct alongside `Label`.

Verify: Compiles; existing call sites still construct the record.

### Step 2 - Render glyph + text cells

Where: `src/CoAttribution.Cli/Tui/Composition/StatusBarComposer.cs`

- Build each `Shortcut` using the `Glyph` plus `Label` from `GlyphSet`; keep `Key.Empty` and `BindKeyToApplication = false`.

Verify: Real key bindings are not intercepted; glyphs render on default terminals.

### Step 3 - Wire glyphs in screens

Where: `src/CoAttribution.Cli/Tui/Views/MainWindow.cs`, `CommitFormView.cs`, `AuthorSelectionView.cs`

- Return `StatusBarKeyBinding` entries with `Glyph` + `Label` sourced from `GlyphSet`.

Verify: Real `Enter`/`Esc` behavior unchanged; status bar shows glyph+label.

## Context pointers

##### Files

- `src/CoAttribution.Cli/Tui/Abstractions/IStatusBarProvider.cs` - extend `StatusBarKeyBinding`.
- `src/CoAttribution.Cli/Tui/Composition/StatusBarComposer.cs` - glyph/text rendering.
- `src/CoAttribution.Cli/Tui/Views/MainWindow.cs`, `CommitFormView.cs`, `AuthorSelectionView.cs` - `GetKeyBindings` providers.

##### ADRs

- `docs/adr/0001-native-aot-constraint.md` - AoT-safe glyph access.

##### Ledger records

- `DECISIONS-CoAttribution-tui-modernization.md#T012` - status-bar split glyph/text cells.
- `DECISIONS-CoAttribution-tui-modernization.md#T008` - glyphs consumed from `GlyphSet`.
- `DECISIONS-CoAttribution-tui-modernization.md#T005` - logging unchanged (no new log region).
- `DECISIONS-CoAttribution-tui-modernization.md#D005` - status-bar shortcuts display-only with glyphs.
- `DECISIONS-CoAttribution-tui-modernization.md#D004` - Unicode glyphs render on default terminals.

## Acceptance criteria

- [ ] `StatusBarKeyBinding` carries a `Glyph` from `GlyphSet` plus `Label` (T012, T008)
- [ ] `StatusBarComposer` renders glyph cell + text cell, keeping `Key=Key.Empty` and `BindKeyToApplication=false` (T012, D005)
- [ ] `MainWindow`/`CommitFormView`/`AuthorSelectionView` `GetKeyBindings` return glyph+label entries (T012)
- [ ] Real key bindings (`Enter`/`Esc`) unchanged; glyphs render on default terminals (D005, D004)

## Dependencies

**Blocked by** - 002-glyphset-config-accessor-di (needs `GlyphSet`), 004-feedbacktoast-title-removal (shared `MainWindow.cs` edit)
