# Consolidated Implementation Plan — CoAttribution TUI mode

> **Context pointer, valid ONLY for the linked spec below.** This plan
> must not be applied to other specifications without explicit
> authorization. Every technical statement in this blueprint that
> satisfies a functional requirement is bound to a `Dxxx`/`Txxx` record
> in the Decision Ledger; if the binding is unclear, do not proceed.
>
> **Linked Spec**: `docs/decisions/DECISIONS-CoAttribution-tui-mode.md` (D001–D019 are the functional requirements; D020 is the session goal).
>
> **Decision Ledger**: `docs/decisions/DECISIONS-CoAttribution-tui-mode.md` (D001–D020, T001–T016).

---

## Project Layout (per T001, T002)

```
src/
  CoAttribution.slnx
  CoAttribution.Cli/
    CoAttribution.Cli.csproj              (T006)
    Program.cs                             (T003, T004, T005)
    Commands/
      RootCommand.cs                       (T003, T016)
    Tui/                                   (T001, T002)
      Composition/
        TuiCompositionRoot.cs              (T003, T004)
        StatusBarComposer.cs               (T013)
      Abstractions/
        IStatusBarProvider.cs              (T013)
      Views/
        MainWindow.cs                      (T016)
        CommitFormView.cs                  (T012)
        AuthorSelectionView.cs             (T010)
      ViewModels/
        AuthorSelectionViewModel.cs        (T007, T008, T009)
        CommitFormViewModel.cs             (T012)
        DraftStore.cs                      (T015)
      Dialogs/
        SetupDialog.cs                     (D007)
        AddAuthorDialog.cs                 (D013)
        MissingHostBlockDialog.cs          (T014)
        PreviewModal.cs                    (T011)
        QuitDialog.cs                      (T015)
  CoAttribution.Lib/                       (no changes; T008, T014 re-use existing Lib services)
```

---

## Per-File Changes

### `src/CoAttribution.Cli/CoAttribution.Cli.csproj`

- Remove the `EnableTui` MSBuild property and the `TUI` define-constant block. [`DECISIONS-CoAttribution-tui-mode.md#T006`]
- Add `Terminal.Gui` v2 `PackageReference` (no version gate; target frameworks unchanged). [`DECISIONS-CoAttribution-tui-mode.md#T006`]
- Verify `IsTrimmable=true` and `IsAoTCompatible=true` still hold after Terminal.Gui v2 is added. [`DECISIONS-CoAttribution-tui-mode.md#T006`]

### `src/CoAttribution.Cli/Program.cs`

- Wire `Cli.Ext.ConfigureServices()` to register TUI services into the existing `ServiceProvider` (composition root, view models, draft store). [`DECISIONS-CoAttribution-tui-mode.md#T004`]
- No TTY detection at this layer; the T003 handler decides. [`DECISIONS-CoAttribution-tui-mode.md#T003`]

### `src/CoAttribution.Cli/Commands/RootCommand.cs`

- Delete the two existing `#if TUI` blocks (lines 10, 23). [`DECISIONS-CoAttribution-tui-mode.md#T006`]
- Add a `Run` method that fires when no subcommand matches. [`DECISIONS-CoAttribution-tui-mode.md#T003`]
- In the `Run` method: TTY check via `Console.IsOutputRedirected || Console.IsInputRedirected`; if either is true, print help and return 0. [`DECISIONS-CoAttribution-tui-mode.md#T005`]
- After TTY check, query `IAuthorRegistry.Count == 0`; if true, show `SetupDialog` before `MainWindow`. [`DECISIONS-CoAttribution-tui-mode.md#T016`]
- Otherwise resolve `TuiCompositionRoot` from DI and call `LaunchAsync()`. [`DECISIONS-CoAttribution-tui-mode.md#T003`, `T004`]

### `src/CoAttribution.Cli/Tui/Composition/TuiCompositionRoot.cs` (new)

- Resolve all TUI view models from the shared `ServiceProvider`. [`DECISIONS-CoAttribution-tui-mode.md#T004`]
- `LaunchAsync()` initializes Terminal.Gui v2 `Application`, builds `MainWindow`, and runs it. [`DECISIONS-CoAttribution-tui-mode.md#T003`]
- Apply the D017 status bar to every screen via `StatusBarComposer`. [`DECISIONS-CoAttribution-tui-mode.md#T013`]

### `src/CoAttribution.Cli/Tui/Composition/StatusBarComposer.cs` (new)

- For each screen implementing `IStatusBarProvider`, wrap the returned key bindings in a Terminal.Gui `StatusBar` widget pinned to the bottom of the screen. [`DECISIONS-CoAttribution-tui-mode.md#T013`]

### `src/CoAttribution.Cli/Tui/Abstractions/IStatusBarProvider.cs` (new)

- Define `IReadOnlyList<StatusBarKeyBinding> GetKeyBindings()`. [`DECISIONS-CoAttribution-tui-mode.md#T013`]

### `src/CoAttribution.Cli/Tui/Views/MainWindow.cs` (new)

- Orchestrates the commit flow: `CommitFormView` → `AuthorSelectionView` → `PreviewModal`. [`DECISIONS-CoAttribution-tui-mode.md#D010`]
- Implements `IStatusBarProvider` returning screen-specific keys (`Esc quit`, `Enter next`). [`DECISIONS-CoAttribution-tui-mode.md#T013`]
- Handles quit via `QuitDialog` per D018. [`DECISIONS-CoAttribution-tui-mode.md#T015`]

### `src/CoAttribution.Cli/Tui/Views/CommitFormView.cs` (new)

- Two fields: `Subject` (single-line `TextField`) and `Body` (multi-line `TextView`). [`DECISIONS-CoAttribution-tui-mode.md#D008`]
- Inline `N/72` counter label next to the subject field; color flips normal → warning at 50+ → red at 72+. [`DECISIONS-CoAttribution-tui-mode.md#T012`]
- Implements `IStatusBarProvider` returning `Enter next`, `Tab next field`, `Esc quit`. [`DECISIONS-CoAttribution-tui-mode.md#T013`]

### `src/CoAttribution.Cli/Tui/Views/AuthorSelectionView.cs` (new)

- Renders the multi-select `CheckBox` list from the view model. [`DECISIONS-CoAttribution-tui-mode.md#D006`]
- Optional type-ahead filter at the top. [`DECISIONS-CoAttribution-tui-mode.md#D006`]
- Toggle switch widget labeled "Advanced view" near the top; basic view is default. [`DECISIONS-CoAttribution-tui-mode.md#T010`]
- `+ Add author` button opens `AddAuthorDialog` in-place; preserves current picks, filter text, and toggle state across the round-trip. [`DECISIONS-CoAttribution-tui-mode.md#D013`]
- Implements `IStatusBarProvider` returning `Space toggle`, `Enter confirm`, `Esc quit`. [`DECISIONS-CoAttribution-tui-mode.md#T013`]

### `src/CoAttribution.Cli/Tui/ViewModels/AuthorSelectionViewModel.cs` (new)

- Resolve authors via `IAuthorRegistry`. [`DECISIONS-CoAttribution-tui-mode.md#D006`]
- Resolve host via `IHostResolver`; if it throws `MissingHostBlockException`, surface that to the screen for D011's dialog. [`DECISIONS-CoAttribution-tui-mode.md#T007`, `D011`]
- Inject a synthetic host row at the top of the list, pre-toggled, showing the resolved `(name, email)`. [`DECISIONS-CoAttribution-tui-mode.md#T007`]
- For each author row, call `CommitOrchestrator.ApplyHostOverride` to compute the displayed `(name, email)`. [`DECISIONS-CoAttribution-tui-mode.md#T008`]
- Re-build rows when the resolved host changes (subscribe to host resolution events). [`DECISIONS-CoAttribution-tui-mode.md#D016`, `D019`]
- Render AI/bot authors with the icon prefix from T009; fall back to `[AI]`/`[Bot]` text prefix when UTF-8 rendering is unavailable. [`DECISIONS-CoAttribution-tui-mode.md#T009`]
- Basic view auto-determines attribution by `ContributorType`; advanced view exposes per-author Co-author / Assisted-by / Default selector with stored default pre-selected. [`DECISIONS-CoAttribution-tui-mode.md#D009`]

### `src/CoAttribution.Cli/Tui/ViewModels/CommitFormViewModel.cs` (new)

- Backs `CommitFormView`: `[Subject]`, `[Body]`, `[SubjectLength]`, `[SubjectColor]`. [`DECISIONS-CoAttribution-tui-mode.md#T012`]
- `SubjectLength` updates on each keystroke; `SubjectColor` flips normal → warning → red per T012 thresholds. [`DECISIONS-CoAttribution-tui-mode.md#T012`]

### `src/CoAttribution.Cli/Tui/ViewModels/DraftStore.cs` (new)

- Persist drafts as JSON under `%LOCALAPPDATA%/CoAttribution/drafts/` (Windows) or `~/.local/share/CoAttribution/drafts/` (POSIX). [`DECISIONS-CoAttribution-tui-mode.md#T015`]
- Use a source-generated `JsonSerializerContext` for AOT safety. [`DECISIONS-CoAttribution-tui-mode.md#T015`]
- `SaveDraftAsync(formState)` and `TryLoadDraftAsync()` round-trip the in-progress form. [`DECISIONS-CoAttribution-tui-mode.md#D018`]
- Auto-create the draft directory on first save. [`DECISIONS-CoAttribution-tui-mode.md#T015`]

### `src/CoAttribution.Cli/Tui/Dialogs/SetupDialog.cs` (new)

- Replaces the v1 file `src/CoAttribution.Cli/Components/Dialogs/SetupDialog.cs` (delete per D012). [`DECISIONS-CoAttribution-tui-mode.md#D012`]
- Guides the user through adding their first author. [`DECISIONS-CoAttribution-tui-mode.md#D007`]
- Implements `IStatusBarProvider`. [`DECISIONS-CoAttribution-tui-mode.md#T013`]

### `src/CoAttribution.Cli/Tui/Dialogs/AddAuthorDialog.cs` (new)

- Replaces the v1 file `src/CoAttribution.Cli/Components/Dialogs/AddAuthorDialog.cs` (delete per D012). [`DECISIONS-CoAttribution-tui-mode.md#D012`]
- Two fields: `Name`, `Email`. [`DECISIONS-CoAttribution-tui-mode.md#D013`]
- On save, calls `IAuthorRegistry.AddAsync` (or equivalent Lib entry). [`DECISIONS-CoAttribution-tui-mode.md#D014`]
- Implements `IStatusBarProvider`. [`DECISIONS-CoAttribution-tui-mode.md#T013`]

### `src/CoAttribution.Cli/Tui/Dialogs/MissingHostBlockDialog.cs` (new)

- Replaces the v1 file `src/CoAttribution.Cli/Components/Dialogs/MissingHostBlockDialog.cs` (delete per D012). [`DECISIONS-CoAttribution-tui-mode.md#D012`]
- Two fields: `Name`, `Email`, with `Save` / `Cancel` buttons. [`DECISIONS-CoAttribution-tui-mode.md#T014`]
- On save, calls `HostBlockWriter`; the registry file must round-trip through `AuthorRegistry` without diff. [`DECISIONS-CoAttribution-tui-mode.md#D011`]
- On success, re-runs the commit flow without leaving the TUI. [`DECISIONS-CoAttribution-tui-mode.md#D011`]
- Implements `IStatusBarProvider`. [`DECISIONS-CoAttribution-tui-mode.md#T013`]

### `src/CoAttribution.Cli/Tui/Dialogs/PreviewModal.cs` (new)

- Modal dialog showing the composed subject, body, and trailers. [`DECISIONS-CoAttribution-tui-mode.md#D010`, `T011`]
- `Confirm` button triggers `git commit`; `Cancel` aborts. [`DECISIONS-CoAttribution-tui-mode.md#D010`]
- Trailer display matches the host-overridden `(name, email)` rendered in the checklist (single source of truth via `CommitOrchestrator.ApplyHostOverride`). [`DECISIONS-CoAttribution-tui-mode.md#T008`, `D010`]
- Implements `IStatusBarProvider`. [`DECISIONS-CoAttribution-tui-mode.md#T013`]

### `src/CoAttribution.Cli/Tui/Dialogs/QuitDialog.cs` (new)

- Triggered by `Esc` / `Ctrl+C` with an in-progress commit form. [`DECISIONS-CoAttribution-tui-mode.md#D018`]
- Three buttons: `Save draft`, `Discard`, `Cancel`. [`DECISIONS-CoAttribution-tui-mode.md#D018`]
- `Save draft` calls `DraftStore.SaveDraftAsync`. [`DECISIONS-CoAttribution-tui-mode.md#T015`]
- Implements `IStatusBarProvider`. [`DECISIONS-CoAttribution-tui-mode.md#T013`]

### `src/CoAttribution.Cli/Components/Windows/MainWindow.cs` (existing v1)

- **Delete the file**; v1 scaffolding is replaced by `Tui/Views/MainWindow.cs`. [`DECISIONS-CoAttribution-tui-mode.md#D012`]

### `src/CoAttribution.Cli/Components/Windows/MessageWindow.cs` (existing v1)

- **Delete the file**; v1 scaffolding is replaced by TUI components under `Tui/`. [`DECISIONS-CoAttribution-tui-mode.md#D012`]

### `src/CoAttribution.Cli/Components/Dialogs/SetupDialog.cs` (existing v1)

- **Delete the file**; replaced by `Tui/Dialogs/SetupDialog.cs`. [`DECISIONS-CoAttribution-tui-mode.md#D012`]

### `src/CoAttribution.Cli/Components/Dialogs/AddAuthorDialog.cs` (existing v1)

- **Delete the file**; replaced by `Tui/Dialogs/AddAuthorDialog.cs`. [`DECISIONS-CoAttribution-tui-mode.md#D012`]

### `src/CoAttribution.Cli/Components/Dialogs/MissingHostBlockDialog.cs` (existing v1)

- **Delete the file**; replaced by `Tui/Dialogs/MissingHostBlockDialog.cs`. [`DECISIONS-CoAttribution-tui-mode.md#D012`]

### `src/CoAttribution.Cli/Components/Dialogs/MissingHostBlockChoice.cs` (existing v1)

- Decide retention or deletion based on whether the v2 dialog still needs a `MissingHostBlockChoice` enum. Most likely **delete**; v2 uses two-field dialog. [`DECISIONS-CoAttribution-tui-mode.md#T014`]

### `src/CoAttribution.Lib/` (no changes required)

- `CommitOrchestrator.ApplyHostOverride` is reused by the TUI for display-time overrides. [`DECISIONS-CoAttribution-tui-mode.md#T008`]
- `HostBlockWriter` is reused by `MissingHostBlockDialog`. [`DECISIONS-CoAttribution-tui-mode.md#D011`, `T014`]
- `IHostResolver` is consumed by the author-selection view model. [`DECISIONS-CoAttribution-tui-mode.md#T007`]
- `IAuthorRegistry` is consumed by the TUI and CLI; its on-disk format is unchanged. [`DECISIONS-CoAttribution-tui-mode.md#D011`, `D016`]

### Build / AOT

- After all changes, run `dotnet build` with NativeAOT analyzers enabled. [`DECISIONS-CoAttribution-tui-mode.md#T006`, `D004`]
- Verify no `IL2026`, `IL2067`, `IL2070`, `IL2072`, `IL3050` warnings remain. [`DECISIONS-CoAttribution-tui-mode.md#T006`]
- Verify non-TTY behavior: `echo "" | dotnet run --project src/CoAttribution.Cli` prints help and exits 0. [`DECISIONS-CoAttribution-tui-mode.md#T005`]

---

## Ledger Reference

Every `Dxxx` / `Txxx` record this blueprint cites:

- `D003` — TUI invocation model
- `D004` — NativeAOT interaction (no `#if TUI`)
- `D006` — Author selection UX (multi-select, filter, AI/bot distinction)
- `D007` — SetupDialog trigger
- `D008` — Subject/body editor UX
- `D009` — Basic/Advanced view toggle
- `D010` — Commit preview / confirmation
- `D011` — Missing-host-block handling
- `D012` — TUI scaffolding strategy (v1 files removed, v2 from scratch)
- `D013` — Author registry menu access
- `D014` — TUI-to-CLI integration (DI-resolved services)
- `D015` — Subject length guidance
- `D016` — Host auto-inclusion in the commit flow
- `D017` — Keybinding discoverability (status bar)
- `D018` — Quit / cancel semantics
- `D019` — Per-author host-override display
- `D020` — Session goal
- `T001` — Project structure (TUI placement)
- `T002` — TUI sub-folder organization (sub-projects)
- `T003` — TUI dispatch integration
- `T004` — DI registration for TUI services
- `T005` — TTY detection
- `T006` — `#if TUI` / `EnableTui` removal
- `T007` — Host row injection in author checklist
- `T008` — Per-host override display
- `T009` — AI/bot vs human visual distinction
- `T010` — Basic/Advanced view toggle widget
- `T011` — Preview modal
- `T012` — Subject length counter
- `T013` — Status bar composition
- `T014` — MissingHostBlockDialog
- `T015` — Draft persistence
- `T016` — SetupDialog trigger
