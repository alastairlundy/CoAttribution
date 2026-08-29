# Implementation Blueprint — CoAttribution TUI Modernization

## Scope Binding

- **Linked Spec**: `docs/decisions/DECISIONS-CoAttribution-tui-modernization.md`
- **Decision Ledger**: `docs/decisions/DECISIONS-CoAttribution-tui-modernization.md`
- **Notice**: This blueprint is a context pointer valid ONLY for the linked spec/ledger above. It must not be applied to other specifications or repos without explicit authorization.

This blueprint turns the design decisions (D001–D008, I001) and the technical decisions (T001–T020) in the Decision Ledger into a file-by-file implementation plan. Every change cites the `Dxxx`/`Txxx` record that drives it.

---

## File Changes

### `src/CoAttribution.Cli/Resources/config.json`

- Add 24-bit TrueColor schemes filling every `VisualRole` (Base, Dialog, HotNormal, HotFocus, Active, ReadOnly, Disabled) using `#RRGGBB` colors [`DECISIONS-CoAttribution-tui-modernization.md#T007`], [`DECISIONS-CoAttribution-tui-modernization.md#D002`].
- Add a `Glyphs` section (Check, Arrow, Warning, KeyEnter, KeyEsc, KeyTab, KeyCtrlEnter) consumed by `GlyphSet` [`DECISIONS-CoAttribution-tui-modernization.md#T008`], [`DECISIONS-CoAttribution-tui-modernization.md#D004`], [`DECISIONS-CoAttribution-tui-modernization.md#T016`].
- Add a `CoAttribution.Fallback` 16-color theme selected automatically when truecolor is unavailable [`DECISIONS-CoAttribution-tui-modernization.md#T010`].

### `src/CoAttribution.Cli/Tui/Composition/ThemeConfigurationHelper.cs`

- At startup, detect terminal truecolor capability and `SwitchTheme` to the fallback theme/scheme when unsupported; keep `ApplyTheme` AoT-safe (no reflection) [`DECISIONS-CoAttribution-tui-modernization.md#T010`], [`DECISIONS-CoAttribution-tui-modernization.md#T007`].

### `src/CoAttribution.Cli/Tui/Composition/GlyphSet.cs` (NEW)

- `public sealed record GlyphSet(...)` parsed once from the config `Glyphs` section via MEC and exposed as a DI singleton; no runtime reflection [`DECISIONS-CoAttribution-tui-modernization.md#T016`], [`DECISIONS-CoAttribution-tui-modernization.md#T008`], [`DECISIONS-CoAttribution-tui-modernization.md#T006`].

### `src/CoAttribution.Cli/Tui/Composition/TuiCompositionRoot.cs`

- Load `GlyphSet` from config and register it as a singleton in the DI container before `Application.Create()` [`DECISIONS-CoAttribution-tui-modernization.md#T016`], [`DECISIONS-CoAttribution-tui-modernization.md#T003`].

### `src/CoAttribution.Cli/Tui/Views/FeedbackToast.cs` (NEW)

- `public sealed class FeedbackToast : View` rendered above content with `Show(string message, FeedbackKind kind)` and `Dismiss()`; auto-dismiss via `Application.MainLoop.AddTimeout` (single AoT-safe timer). `Window.Title` is never mutated [`DECISIONS-CoAttribution-tui-modernization.md#T017`], [`DECISIONS-CoAttribution-tui-modernization.md#T009`], [`DECISIONS-CoAttribution-tui-modernization.md#D003`].

### `src/CoAttribution.Cli/Tui/Views/MainWindow.cs`

- Remove the `Title` mutation in `RunCommitAsync` (lines 297/302/307) and call `FeedbackToast.Show(...)` instead; keep `Title` a stable identity string. Own and add the `FeedbackToast` above the active screen [`DECISIONS-CoAttribution-tui-modernization.md#T009`], [`DECISIONS-CoAttribution-tui-modernization.md#T017`], [`DECISIONS-CoAttribution-tui-modernization.md#D003`].
- `GetKeyBindings()` supplies glyph + label via `GlyphSet` [`DECISIONS-CoAttribution-tui-modernization.md#T012`], [`DECISIONS-CoAttribution-tui-modernization.md#T008`].

### `src/CoAttribution.Cli/Tui/Abstractions/IStatusBarProvider.cs`

- Extend `StatusBarKeyBinding` to carry a `Glyph` (from `GlyphSet`) plus `Label`, enabling split glyph/text cells in the status bar [`DECISIONS-CoAttribution-tui-modernization.md#T012`], [`DECISIONS-CoAttribution-tui-modernization.md#T008`].

### `src/CoAttribution.Cli/Tui/Composition/StatusBarComposer.cs`

- Render each `Shortcut` with a dedicated glyph cell and a text cell using `GlyphSet`; keep `Key = Key.Empty` and `BindKeyToApplication = false` so real bindings are unchanged [`DECISIONS-CoAttribution-tui-modernization.md#T012`], [`DECISIONS-CoAttribution-tui-modernization.md#D005`], [`DECISIONS-CoAttribution-tui-modernization.md#T005`], [`DECISIONS-CoAttribution-tui-modernization.md#T008`].

### `src/CoAttribution.Cli/Tui/Views/CommitFormView.cs` & `AuthorSelectionView.cs` (status-bar providers)

- Update `GetKeyBindings()` on each to return `StatusBarKeyBinding` entries with glyph + label from `GlyphSet` [`DECISIONS-CoAttribution-tui-modernization.md#T012`], [`DECISIONS-CoAttribution-tui-modernization.md#T008`].

### `src/CoAttribution.Cli/Tui/ViewModels/AuthorListRow.cs` (NEW)

- `public sealed class AuthorListRow` with `Id`, `DisplayLabel`, `IsSelected`, `SelectedAttributionType`, `IsHostRow`, mapped from the existing `AuthorRow` [`DECISIONS-CoAttribution-tui-modernization.md#T018`], [`DECISIONS-CoAttribution-tui-modernization.md#T013`].

### `src/CoAttribution.Cli/Tui/ViewModels/AuthorSelectionViewModel.cs`

- Expose an `IReadOnlyList<AuthorListRow>` row source for the `ListView`; preserve filter, multi-select, and advanced attribution cycling semantics [`DECISIONS-CoAttribution-tui-modernization.md#T013`], [`DECISIONS-CoAttribution-tui-modernization.md#T018`], [`DECISIONS-CoAttribution-tui-modernization.md#D006`].

### `src/CoAttribution.Cli/Tui/Views/AuthorSelectionPanelView.cs` (NEW)

- `public sealed class AuthorSelectionPanelView : View` with a left filterable `ListView` `FrameView` (bound to `AuthorListRow`) and a right selected/attribution `FrameView`; preserves the CommitForm→AuthorSelection→Preview flow [`DECISIONS-CoAttribution-tui-modernization.md#T020`], [`DECISIONS-CoAttribution-tui-modernization.md#T014`], [`DECISIONS-CoAttribution-tui-modernization.md#D007`], [`DECISIONS-CoAttribution-tui-modernization.md#T011`].

### `src/CoAttribution.Cli/Tui/Views/AuthorSelectionView.cs`

- Replace the manual `CheckBox` stack (`RebuildCheckboxes`) with a `ListView` bound to `AuthorListRow`, selection shown via `GlyphSet.Check`; host the panes inside `AuthorSelectionPanelView`; preserve multi-select, filter, advanced attribution cycling, `Enter`/`Esc` behavior, and screen flow [`DECISIONS-CoAttribution-tui-modernization.md#T013`], [`DECISIONS-CoAttribution-tui-modernization.md#T014`], [`DECISIONS-CoAttribution-tui-modernization.md#T018`], [`DECISIONS-CoAttribution-tui-modernization.md#T020`], [`DECISIONS-CoAttribution-tui-modernization.md#D006`], [`DECISIONS-CoAttribution-tui-modernization.md#D001`], [`DECISIONS-CoAttribution-tui-modernization.md#I001`].

### `src/CoAttribution.Cli/Tui/Views/CommitFormSectionsView.cs` (NEW)

- `public sealed class CommitFormSectionsView : View` hosting a `FrameView`(Subject) and `FrameView`(Body) that wrap the existing commit-form controls; behavior unchanged [`DECISIONS-CoAttribution-tui-modernization.md#T019`], [`DECISIONS-CoAttribution-tui-modernization.md#T011`], [`DECISIONS-CoAttribution-tui-modernization.md#D008`].

### `src/CoAttribution.Cli/Tui/Views/CommitFormView.cs`

- Move the Subject/Body controls into `CommitFormSectionsView`; control behavior (counters, hard caps, Tab navigation) unchanged [`DECISIONS-CoAttribution-tui-modernization.md#T019`], [`DECISIONS-CoAttribution-tui-modernization.md#D008`], [`DECISIONS-CoAttribution-tui-modernization.md#D001`].

### `src/CoAttribution.Cli/CoAttribution.Cli.csproj`

- Confirm `LangVersion` = C# 14, `TargetFramework` = `net10.0`, and NativeAOT settings (`IsAoTCompatible`, `IsTrimmable`, `PublishAot`) remain enabled; no TFM downgrade [`DECISIONS-CoAttribution-tui-modernization.md#T001`], [`DECISIONS-CoAttribution-tui-modernization.md#T002`], ADR 0001.

### `src/CoAttribution.Cli.Tests/` (NEW or extend existing test project)

- Add TUnit-based headless construction/behavior tests for `GlyphSet`, `FeedbackToast`, `CommitFormSectionsView`, `AuthorSelectionPanelView`, and `AuthorListRow` mapping; assert trim/AoT safety where feasible [`DECISIONS-CoAttribution-tui-modernization.md#T004`], [`DECISIONS-CoAttribution-tui-modernization.md#T002`], [`DECISIONS-CoAttribution-tui-modernization.md#T016`], [`DECISIONS-CoAttribution-tui-modernization.md#T017`], [`DECISIONS-CoAttribution-tui-modernization.md#T018`], [`DECISIONS-CoAttribution-tui-modernization.md#T019`], [`DECISIONS-CoAttribution-tui-modernization.md#T020`].

---

## Ledger Reference

**Design decisions (spec):**

- `D001` — Modernize TUI aesthetically, retain all functionality.
- `D002` — TrueColor neutral palette filling all `VisualRole`s, graceful degradation.
- `D003` — Dedicated feedback banner; do not mutate `Window.Title`.
- `D004` — Unicode-only glyphs in `config.json` `Glyphs.*`.
- `D005` — Status-bar shortcuts display-only with glyphs (`Key.Empty` unchanged).
- `D006` — `ListView` + glyph checks replace the checkbox stack; preserve multi-select/filter/attribution.
- `D007` — Split-panel author selection (left list, right selected/attribution).
- `D008` — `FrameView` sectioning by role.
- `I001` — `D001` permits UX changes while functionality is retained (steers `D007` to split-panel).

**Technical decisions:**

- `T001` — C# 14 on .NET 10 LTS; NativeAOT.
- `T002` — Terminal.Gui v2 rendering engine.
- `T003` — Keep DI + MEC config + CommunityToolkit.Mvvm.
- `T004` — TUnit test framework.
- `T005` — Logging stays as-is (`FileLogger`).
- `T006` — Glyph/theme values sourced AoT-safely (no reflection).
- `T007` — TrueColor schemes inline as `#RRGGBB` in config.json.
- `T008` — `Glyphs.*` config block + record accessor.
- `T009` — Transient toast overlay for commit feedback.
- `T010` — Capability-gated truecolor fallback swap.
- `T011` — FrameView sectioning via composite sub-views.
- `T012` — Status-bar split glyph/text cells.
- `T013` — `ListView` + glyph selection migration.
- `T014` — Two-pane FrameView split-panel.
- `T015` — Presentation types stay in `CoAttribution.Cli.Tui`; Lib stays Terminal.Gui-free.
- `T016` — `GlyphSet` record loaded from config.json.
- `T017` — `FeedbackToast : View` with `MainLoop` timeout.
- `T018` — `AuthorListRow` DTO mapped from `AuthorRow`.
- `T019` — `CommitFormSectionsView` composite sub-view.
- `T020` — `AuthorSelectionPanelView` two-pane sub-view.
