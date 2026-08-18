# Decision Ledger — CoAttribution TUI mode

### [D001] — session goal

- **Driver**: Users not yet up to speed on the CLI syntax, who have forgotten it, or who otherwise don't want to use the CLI.
- **Resolved Answer**: "I want an easy to use TUI system for individuals who aren't yet up to speed on the CLI syntax, have forgotten/don't remember the syntax, or otherwise don't want to use the CLI interface."
- **Normalized Requirement**: CoAttribution shall provide a TUI mode that lowers the interaction barrier below the current CLI for non-expert users.
- **Constraints**: None.

### [D002] — TUI feature scope

- **Driver**: Users not up to speed on the CLI syntax shouldn't need the CLI for setup.
- **Resolved Answer**: "Option B"
- **Normalized Requirement**: The TUI shall cover both the commit flow and author registry management (`author add/remove/list`); setup (`init`, `config`) remains CLI-only.
- **Constraints**: `init` and `config` are not exposed in the TUI; TUI registry edits must produce the same on-disk state as the equivalent CLI commands.

### [D003] — invocation model

- **Driver**: New users get the TUI by default with no learning barrier.
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: `co-attr` invoked with no subcommand and no arguments shall launch the TUI.
- **Constraints**: Behavior in non-TTY contexts is deferred to a separate branch.

### [D004] — NativeAOT interaction

- **Driver**: Terminal.Gui v2 has removed the NativeAOT warning noise and is AOT-compatible.
- **Resolved Answer**: "Terminal.Gui v2 in the latest updates remove the NativeAoT warning noise and is AOT compatible. Remove The #if TUI gate."
- **Normalized Requirement**: The TUI shall be part of the default build with no `#if TUI` conditional compilation; the `EnableTui` MSBuild property and `TUI` define constant shall be removed.
- **Constraints**: NativeAOT compatibility must continue to hold; the project must still build with `IsTrimmable=true` and `IsAoTCompatible=true`.

### [D005] — non-TTY behavior

- **Driver**: Predictable, script-friendly behavior for `co-attr` when no interactive terminal is available.
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: When `co-attr` is invoked with no subcommand and stdout or stdin is not a TTY, it shall print the same help text it would otherwise show and exit with code 0.
- **Constraints**: None.

### [D006] — author selection UX

- **Driver**: New users need to pick authors quickly while still being able to handle larger registries.
- **Resolved Answer**: "Multi select checklist with Optional filter - Filter can narrow down list of authors. However I want to make it easy for users to distinguish which authors are AI/bots and which are not."
- **Normalized Requirement**: Author selection in the TUI shall be a multi-select checklist of all registered authors with an optional type-ahead filter that narrows the list; AI/bot authors shall be visually distinguished from human authors in the rendered list.
- **Constraints**: None.

### [D007] — SetupDialog trigger

- **Driver**: First-time and empty-registry users should be guided into setup without manual discovery.
- **Resolved Answer**: "Option B"
- **Normalized Requirement**: The SetupDialog shall be shown whenever the loaded author registry has zero entries.
- **Constraints**: None.

### [D008] — subject/body editor UX

- **Driver**: New users need a clear structure without learning git's first-line convention.
- **Resolved Answer**: "Option B"
- **Normalized Requirement**: The TUI commit editor shall expose two separate fields — a single-line subject field and a multi-line body text view.
- **Constraints**: None.

### [D009] — Co-author vs Assisted-by attribution selection

- **Driver**: New users should not have to learn the Co-authored-by vs Assisted-by distinction, while power users still need per-commit control.
- **Resolved Answer**: "I want a view option/toggle near the top of the UI that toggles between a basic view and the advanced view. The basic view shows the view described by Option A. The advanced view shows the view described by Option B but with the default value selected by default. The default view should be the basic view unless toggled to Advanced."
- **Normalized Requirement**: The TUI shall expose a view toggle near the top of the author-selection UI that switches between a basic view (auto-determination by `ContributorType` per Option A) and an advanced view (per-author Co-author / Assisted-by / Default selector with the stored default pre-selected per Option B); the basic view shall be the default landing state.
- **Constraints**: The toggle must be visually discoverable near the top of the UI; the basic view is the default state on every entry into the selection screen.

### [D010] — commit preview / confirmation

- **Driver**: Users who can't easily undo git history need a last-chance review before a real commit lands.
- **Resolved Answer**: "Option B"
- **Normalized Requirement**: After the commit form is submitted, the TUI shall display a preview modal showing the composed subject, body, and trailers; the user must confirm before `git commit` runs.
- **Constraints**: None.

### [D011] — missing-host-block handling

- **Driver**: Users who don't want to use the CLI shouldn't be ejected from the TUI mid-commit.
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: When `MissingHostBlockException` fires during the commit flow, the TUI shall display a guided `MissingHostBlockDialog` that lets the user type the missing `(name, email)` for the host, write the host block to the registry, and retry the commit — all without leaving the TUI.
- **Constraints**: The in-TUI host-block writer must call `HostBlockWriter` (or equivalent) and produce a registry file that round-trips through `AuthorRegistry` without diff.

### [D012] — TUI scaffolding strategy

- **Driver**: The existing scaffolding targets Terminal.Gui v1; D004 mandates Terminal.Gui v2 with no `#if TUI` gate.
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: The TUI scaffolding (MainWindow, MessageWindow, SetupDialog, AddAuthorDialog, MissingHostBlockDialog, and any new components) shall be written from scratch against Terminal.Gui v2 idioms; the existing v1 files shall be removed.
- **Constraints**: None.

### [D013] — author registry menu access

- **Driver**: Users in the commit flow should be able to add the author they just discovered they need without leaving the screen.
- **Resolved Answer**: "Option B"
- **Normalized Requirement**: The author-selection screen shall expose a `+ Add author` button that opens `AddAuthorDialog` in-place; the top-level menu shall not duplicate the action.
- **Constraints**: State on the selection screen (current picks, filter text, view toggle) must be preserved across the add-author round-trip.

### [D014] — TUI-to-CLI integration path

- **Driver**: TUI actions must feel instant and stay AOT-friendly without forking business logic.
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: TUI components shall resolve `ICommitOrchestrator`, `IAuthorRegistry`, `HostBlockWriter`, and other Lib services directly via DI; CLI commands remain a separate surface and are not invoked as subprocesses by the TUI.
- **Constraints**: TUI and CLI must not share parse or I/O code; refactoring Lib services for TUI use must not regress CLI behavior.

### [D015] — subject length guidance

- **Driver**: New users discover git's subject-length convention only when `git commit` rejects — too late.
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: The TUI subject field shall show a live `N/72` character counter that turns warning at 50+ and red at 72+.
- **Constraints**: None.

### [I001] — D016 premise check

- **Prompt**: "For D016 — default authors in the commit flow: pick an option, hybridize, or provide your own answer."
- **User Response**: "I didn't realize Default Authors is a functionality that CoAttribution has. Can you double check this is actually the case?"
- **Resolution**: Code verification (`AttributionType.cs`, `AttributionPolicy.cs`, `GitCoAuthor.cs`, `CommitOrchestrator.cs`) confirmed there is no "Default Authors" list concept; the closest analog is the host key being auto-merged into `CommitRequest.DefaultIds`. Branch D016 is re-asked with the corrected framing focused on the host's auto-inclusion.
- **Notes**: D016 was never recorded; the branch is still open. Re-ask in the next round.

### [D017] — keybinding discoverability

- **Driver**: New users need keyboard shortcuts visible without searching.
- **Resolved Answer**: "Option A but the status bar is pinned to the bottom of the UI."
- **Normalized Requirement**: The TUI shall show a status bar pinned to the bottom of every screen, listing the keys relevant to the current screen (e.g. `Space toggle`, `Enter confirm`, `Esc cancel`).
- **Constraints**: The status bar must remain pinned (not floating, not pop-up) at the bottom of the viewport.

### [D018] — quit / cancel semantics

- **Driver**: Accidental Esc should not punish users for half-typed work.
- **Resolved Answer**: "Option B"
- **Normalized Requirement**: When the user initiates quit (Esc / Ctrl+C) with an in-progress commit form, the TUI shall display a dialog offering Save draft / Discard / Cancel; the Save draft option shall persist the in-progress commit form to a draft file the TUI can resume from on next launch.
- **Constraints**: Drafts must survive a session restart; draft cleanup rules (age, count) are out of scope for this branch.

### [I002] — D016 Option A mechanics clarification

- **Prompt**: "For D016 — host auto-inclusion in the commit flow: pick an option, hybridize, or provide your own answer."
- **User Response**: "What does Option A entail as 'pre-checked entry in the checklist'? This sounds like it would bundle a bunch of choices in one view/screen."
- **Resolution**: Agent clarified that Option A means the host appears as a single row in the existing author multi-select checklist (the same screen established by D006), with its checkbox pre-toggled — identical mechanics to any other author row, including space-toggle to opt out. D016 is re-asked with the cleaner description.
- **Notes**: D016 record pending; will be backfilled when user resolves the re-asked branch.

### [I003] — D016 bundling clarification

- **Prompt**: "For D016 — host auto-inclusion in the commit flow (re-asked): pick an option, hybridize, or provide your own answer."
- **User Response**: "How does Option A handle a scenario where the same author is defined with differently for multiple hosts? You haven't addressed my bundling concern."
- **Resolution**: Agent acknowledged the bundling concern was under-addressed. D016 is re-scoped to the host entry itself (which is a single resolved row, e.g. "github"); a new branch D019 is opened for the related question of how authors with per-host overrides are displayed in the selection screen (where multiple identities per author genuinely exist).
- **Notes**: D016 record still pending; D019 is a new branch about per-author host-override display in the checklist.

### [D016] — host auto-inclusion in the commit flow

- **Driver**: Users should see exactly which host identity lands in the trailer.
- **Resolved Answer**: "Option B"
- **Normalized Requirement**: The resolved host shall appear as a single pre-toggled row in the author multi-select checklist showing the exact `(name, email)` that will land in the trailer; users can space-toggle to opt out per commit.
- **Constraints**: The host row text reflects the current resolved host; if the resolved host changes mid-session, the row updates accordingly.

### [D019] — per-author host-override display in the selection screen

- **Driver**: The checklist and the final preview must agree on every `(name, email)`.
- **Resolved Answer**: "Option B"
- **Normalized Requirement**: Each author row in the multi-select checklist shall show the host-overridden `(name, email)` when an override exists for the resolved host, otherwise the base identity; the row text shall update when the resolved host changes.
- **Constraints**: Per-host overrides are applied at display time so what the user sees matches what the D010 preview modal shows; behavior is identical to the silent application in `CommitOrchestrator.ApplyHostOverride` (Lib/CommitOrchestrator.cs:100-122).

### [D020] — session goal

- **Driver**: The user wants both the tech decisions resolved and a Consolidated Implementation Plan at the end, in sequence — the plan is the handoff to ticket generation.
- **Resolved Answer**: "Both, in sequence" — "Both: resolve tech decisions first, then draft the Consolidated Implementation Plan at the end."
- **Normalized Requirement**: The session shall resolve the foundation and spec-driven TDPs for the CoAttribution TUI, then produce a Consolidated Implementation Plan consumable by `spec-to-tickets`.
- **Constraints**: This session covers foundation (TUI framework, layout/lifecycle, project structure) and TDPs only — the functional spec (D001–D019) is locked and not re-litigated.

### [T001] — project structure (TUI placement)

- **Driver**: The user wants a single-binary AOT surface and a direct mapping to D003 (TUI is the default launch mode of the CLI binary).
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: The TUI code shall live in a sub-folder of the existing Cli project at `src/CoAttribution.Cli/Tui/`; the solution remains a single binary, single .csproj for the UI.
- **Constraints**: The TUI and CLI shall not share parse or I/O code (per D014); AOT trim/AO​T analysis must continue to hold for the unified project.
- **Cites**: D003, D004, D012, D014, D020

### [T002] — TUI sub-folder organization (sub-projects scope)

- **Driver**: MVVM discipline gives each layer one job, and D017's cross-screen status bar needs a clear home in one responsibility bucket.
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: The TUI sub-folder shall be organized by responsibility with sub-folders `Views/`, `Dialogs/`, `ViewModels/`, `Composition/` (or similar) under `src/CoAttribution.Cli/Tui/`.
- **Constraints**: D012 components must be findable (no orphan files); D017 status bar must live in one of the responsibility buckets (e.g. `Composition/`); cross-screen primitives cannot leak across feature boundaries.
- **Cites**: T001, D012, D014, D017, D020

### [T003] — TUI dispatch integration

- **Driver**: Idiomatic DotMake keeps the parser as the single source of truth for "is a subcommand present".
- **Resolved Answer**: "B"
- **Normalized Requirement**: The TUI launch shall be implemented as a custom `Run` method on `RootCommand` that fires when no subcommand matches; the method delegates to the TUI composition root.
- **Constraints**: TUI install must respect D005 (non-TTY exits 0 with help); handler must not regress existing CLI subcommands; D014 (DI-resolved services) must hold.
- **Cites**: D003, D005, D014, T001, D020

### [T004] — DI registration for TUI services

- **Driver**: A single shared container matches D014's "TUI resolves Lib services via DI" guidance and re-uses the existing `Cli.Ext.ConfigureServices()` pattern.
- **Resolved Answer**: "A"
- **Normalized Requirement**: TUI services shall be registered alongside existing CLI services in the same `ServiceProvider` configured by `Cli.Ext.ConfigureServices()`.
- **Constraints**: TUI and CLI must not share parse or I/O code (D014); TUI-only services must not be accidentally resolved by CLI commands at parse time.
- **Cites**: D014, T001, T003, D020

### [T005] — TTY detection

- **Driver**: NativeAOT-safe, zero-dep, and accurate enough for D005's "any stream is non-TTY → print help" rule.
- **Resolved Answer**: "A"
- **Normalized Requirement**: TTY detection shall use `Console.IsOutputRedirected || Console.IsInputRedirected`; if either is true, the RootCommand handler prints help and exits 0 instead of launching the TUI.
- **Constraints**: D005 must be honored; the check must not regress when both streams are TTYs; NativeAOT compatibility must hold (D004).
- **Cites**: D005, T003, D004, D020

### [T006] — `#if TUI` / `EnableTui` MSBuild removal

- **Driver**: D004 is explicit (no `#if TUI`, no `EnableTui`); a single-build artifact is the lowest-friction path.
- **Resolved Answer**: "A"
- **Normalized Requirement**: The `EnableTui` MSBuild property and `TUI` define constant shall be removed from the Cli project; every `#if TUI` block in `.cs` files shall be deleted (with the dead branch removed, not commented out).
- **Constraints**: NativeAOT compatibility must continue to hold (D004); the build must succeed with a single configuration; no MSBuild property can reintroduce the gate.
- **Cites**: D004, D012, T001, D020

### [T007] — host row injection in author checklist

- **Driver**: D016 explicitly says the host is a single row in the existing checklist with the same space-toggle mechanics.
- **Resolved Answer**: "A"
- **Normalized Requirement**: The author-selection view model shall resolve the host via `IHostResolver` and add a synthetic "host" row to the underlying list; the row text shows the resolved `(name, email)` and is pre-toggled.
- **Constraints**: Host resolution failure must surface as `MissingHostBlockException` and trigger D011's dialog; the host row must update when the resolved host changes (D016).
- **Cites**: D016, D006, D011, D020

### [T008] — per-host override display

- **Driver**: Single source of truth for "what identity lands in the trailer".
- **Resolved Answer**: "A"
- **Normalized Requirement**: Per-host overrides shall be applied at display time by calling `CommitOrchestrator.ApplyHostOverride` from the author-selection view model when building the row text.
- **Constraints**: The displayed `(name, email)` must match what `CommitOrchestrator` will commit (D010); the row text must update when the resolved host changes (D019).
- **Cites**: D019, D010, D006, D020

### [T009] — AI/bot vs human visual distinction

- **Driver**: AI/bot authors must be visually distinguished in a way that works across terminals, including non-UTF-8 environments.
- **Resolved Answer**: "Use Text only prefix badge as a fallback where Glyphs don't render properly. Use the Icon prefix by default if supported."
- **Normalized Requirement**: AI/bot authors shall be rendered with an icon prefix by default (e.g. `🤖` or `★`); when the terminal cannot render the glyph (UTF-8 detection), the row shall fall back to a text-prefix badge (`[AI]` or `[Bot]`).
- **Constraints**: The fallback must be automatic; the user must never see a `?` or replacement character; the prefix must not collide with the author's name text.
- **Cites**: D006, D020

### [T010] — basic/advanced view toggle

- **Driver**: User prefers compact toggle widget over two radio buttons.
- **Resolved Answer**: "B"
- **Normalized Requirement**: The basic/advanced view toggle shall be a single on/off toggle switch widget labeled "Advanced view" placed near the top of the author-selection UI; basic view is the default landing state.
- **Constraints**: The toggle must be visually discoverable near the top of the UI (D009); the basic view must be the default landing state (D009); the toggle state must be visible without a key press.
- **Cites**: D009, D020

### [T011] — preview modal

- **Driver**: D010 requires "must confirm before `git commit` runs" — a modal dialog enforces the gate.
- **Resolved Answer**: "A"
- **Normalized Requirement**: After the commit form is submitted, the TUI shall display a Terminal.Gui modal dialog showing the composed subject, body, and trailers; the user must press a `Confirm` button (or `Cancel` to abort) before `git commit` runs.
- **Constraints**: The dialog must block the form; long trailer lists must scroll within the dialog; the dialog must not regress the Lib commit pipeline.
- **Cites**: D010, D014, D020

### [T012] — subject length counter

- **Driver**: D015 names a "live `N/72` counter" — inline co-location is the most direct reading.
- **Resolved Answer**: "A"
- **Normalized Requirement**: The subject field shall display a right-aligned `N/72` label next to the field; the label color flips normal → warning at 50+ → red at 72+.
- **Constraints**: The counter must update live on each keystroke; the colors must be visible in the default Terminal.Gui color scheme; the label must not push the field out of the visible region.
- **Cites**: D015, D020

### [T013] — status bar composition

- **Driver**: A typed contract that every screen must satisfy gives the helper a compile-time check.
- **Resolved Answer**: "A"
- **Normalized Requirement**: Every screen shall implement `IStatusBarProvider` returning a list of key bindings; the composition root wraps the list in a Terminal.Gui `StatusBar` widget pinned to the bottom of the screen.
- **Constraints**: The bar must be pinned to the bottom (D017); a missing implementation must be a compile-time error; the widget must respect Terminal.Gui v2 idioms.
- **Cites**: D017, D012, D020

### [T014] — MissingHostBlockDialog

- **Driver**: D011 names "type the missing `(name, email)`" — two fields is the most direct mapping.
- **Resolved Answer**: "A"
- **Normalized Requirement**: The TUI shall display a `MissingHostBlockDialog` with two fields (`Name`, `Email`) and `Save` / `Cancel` buttons; on save, the dialog calls `HostBlockWriter` and re-runs the commit flow without leaving the TUI.
- **Constraints**: The registry file must round-trip through `AuthorRegistry` without diff (D011); email validation must be inline; the dialog must honor D017's status bar.
- **Cites**: D011, D014, D017, D020

### [T015] — draft persistence

- **Driver**: NativeAOT-safe with source-generated JSON, follows platform conventions, and separates session state from the durable registry.
- **Resolved Answer**: "A"
- **Normalized Requirement**: The draft shall be persisted as JSON under `%LOCALAPPDATA%/CoAttribution/drafts/` on Windows (or `~/.local/share/CoAttribution/drafts/` on POSIX); the dump uses a source-generated JSON context for AOT safety.
- **Constraints**: Drafts must survive a session restart (D018); the draft directory must be auto-created on first save; cleanup rules (age, count) are out of scope per D018.
- **Cites**: D018, D004, D020

### [T016] — SetupDialog trigger

- **Driver**: D007 fires whenever the registry is empty — the same place that loads the registry is the right gate.
- **Resolved Answer**: "A"
- **Normalized Requirement**: After TTY validation, the T003 RootCommand handler shall query `IAuthorRegistry.Count == 0`; if true, it shows `SetupDialog` before `MainWindow`; otherwise it shows `MainWindow` directly.
- **Constraints**: The check must happen after TTY validation (D005, T005); the dialog must be modal; the user must end setup with a non-empty registry or abort the TUI.
- **Cites**: D007, T003, T004, D020

<!-- next-d: D021 -->
<!-- next-t: T017 -->
<!-- next-i: I004 -->