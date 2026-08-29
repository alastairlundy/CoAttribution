# Decision Ledger — CoAttribution TUI Modernization

### [D001] - session goal

- **Driver**: the user wants the TUI visually modernized without losing any current capability.
- **Resolved Answer**: "Modernize CoAttribution's TUI to be more aesthetically pleasing whilst retaining the same functionality that the current TUI supports"
- **Normalized Requirement**: The session shall produce TUI design decisions that improve visual/aesthetic quality (theme, layout hierarchy, feedback, discoverability, glyphs) without removing or changing any currently supported feature, control, key binding, or screen in the commit flow.
- **Constraints**: Must retain the existing screen flow (CommitFormView -> AuthorSelectionView -> PreviewModal), existing key bindings, and NativeAOT compatibility (ADR 0001). No new domain functionality beyond aesthetic/feedback presentation.

### [D002] - theme color depth & identity

- **Driver**: the user wants a modern, showcase-app-level theme without inventing an undefined brand identity.
- **Resolved Answer**: "Option A — TrueColor neutral palette"
- **Normalized Requirement**: The TUI theme shall use 24-bit TrueColor with a neutral modern palette and fill all `VisualRole`s (HotNormal, HotFocus, Active, ReadOnly, Disabled, plus Base/Dialog roles), degrading gracefully on non-truecolor terminals.
- **Constraints**: No branded/project palette introduced (none is defined in `GLOSSARY.md`). Inherits `D001` functionality and NativeAOT constraints.

### [D003] - commit feedback surface

- **Driver**: the user wants commit outcome communicated clearly without overloading the window title.
- **Resolved Answer**: "Option A — Dedicated feedback banner"
- **Normalized Requirement**: Commit success/failure/error shall be shown in a dedicated feedback banner/status region, not by mutating `Window.Title`; the title shall remain a stable identity string.
- **Constraints**: Must not add a workflow step or new screen (per `D001`). Commit-flow behavior unchanged.

### [D004] - glyph vocabulary

- **Driver**: the user wants modern glyphs without assuming a Nerdfont-capable terminal or breaking NativeAOT.
- **Resolved Answer**: "Option A — Unicode-only glyphs"
- **Normalized Requirement**: Icons/indicators (selection, check, arrow) shall use plain Unicode symbols defined in `config.json` `Glyphs.*`; no Nerdfont dependency.
- **Constraints**: Must render on default terminals (per `D001`/ADR 0001 portability). Glyph set limited to widely-supported Unicode.

### [D005] - status bar key bindings

- **Driver**: the user wants key discoverability without changing behavior.
- **Resolved Answer**: "Option A — Display-only with glyphs"
- **Normalized Requirement**: Status-bar shortcuts shall remain display-only (`Key.Empty`) but render key glyphs/labels for discoverability; real key bindings are unchanged elsewhere.
- **Constraints**: Must not intercept Enter/Esc from focused views (preserves `D001` behavior). Inherits the `D004` glyph decision.

### [D006] - author selection control

- **Driver**: the user wants a modern list feel while preserving multi-select author selection.
- **Resolved Answer**: "Option A — ListView + glyph checks"
- **Normalized Requirement**: The manual checkbox stack shall be replaced by a `ListView` whose selection state is shown via a Unicode glyph (`D004`); multi-select, filter, and advanced-view attribution cycling must be preserved.
- **Constraints**: Must preserve existing author-selection functionality (per `D001`). Attribution cycling behavior unchanged.

### [I001] - functionality vs UX scope of D001

- **Prompt**: "Recommendation: Option A - Sequential, restyled. Reasoning: Sequential restyled aligns with your goal of retaining the same functionality D001 states, delivering aesthetic gain without flow change..."
- **User Response**: "D001 doesn't forbid changes to the UX. It says keep the same functionality, not keep the UX exactly the same."
- **Resolution**: Corrects the reasoning behind the `D007` recommendation; `D001` permits UX changes as long as functionality is retained, so split-panel (Option B) is admissible. Steers `D007` recommendation from A to B.
- **Notes**: Distinguishes "functionality" (features, flow, key bindings, screens) from "UX" (visual arrangement). `D001`'s Resolved Answer is unchanged.

### [D007] - layout architecture

- **Driver**: the user wants a showcase-app split-panel aesthetic while preserving functionality.
- **Resolved Answer**: "Option B — Split-panel selection"
- **Normalized Requirement**: The AuthorSelection screen shall use a split-panel layout (left filterable list, right selected/attribution panel) while preserving the CommitForm -> AuthorSelection -> Preview flow, multi-select, filter, and key bindings.
- **Constraints**: Must preserve functionality per `D001`/`I001`. Layout-implementation risk belongs to `code-implementation-grilling`.

### [D008] - visual hierarchy sectioning

- **Driver**: the user wants clear visual grouping of related controls for a modern hierarchy.
- **Resolved Answer**: "Option A — FrameView by role"
- **Normalized Requirement**: Forms (commit form, author selection) shall group controls into labeled `FrameView` sections by role (Subject/Body; Identity/Selection/Attribution) without altering control behavior.
- **Constraints**: Must not alter control behavior or functionality (per `D001`). Purely presentational grouping.

### [T001] - primary language & target framework

- **Driver**: the user wants the TUI on a current, supported, AoT-capable stack while refreshing visuals.
- **Resolved Answer**: "C# 14 on .NET 10 LTS"
- **Normalized Requirement**: The TUI shall be built on C# 14 targeting `net10.0` (LTS), preserving NativeAOT compatibility (ADR 0001).
- **Constraints**: Must remain NativeAOT-compatible; do not downgrade the TFM.
- **Cites**: D001

### [T002] - TUI framework/runtime

- **Driver**: the user wants to keep the existing view implementations and the design primitives they assume.
- **Resolved Answer**: "Option A — Terminal.Gui v2"
- **Normalized Requirement**: The TUI shall continue to use Terminal.Gui v2 as its rendering engine.
- **Constraints**: Must support `ListView`, `FrameView`, `StatusBar`, and `VisualRole`/`Scheme` APIs assumed by D002/D006/D007/D008.
- **Cites**: D001, D002, D006, D007, D008

### [T003] - supporting dependencies & composition

- **Driver**: the user wants to preserve current behavior and the composition used by theme/banner code.
- **Resolved Answer**: "Option A — Current DI + MEC config"
- **Normalized Requirement**: The TUI shall keep Microsoft.Extensions.DependencyInjection, MEC JSON configuration, and CommunityToolkit.Mvvm.
- **Constraints**: `TuiCompositionRoot` and `ThemeConfigurationHelper` must remain functional under trimming/AoT.
- **Cites**: D001, D003, D005

### [T004] - test/verification approach

- **Driver**: the user wants a consistent, modern test framework for verifying TUI changes under NativeAOT.
- **Resolved Answer**: "Test framework should use TUnit."
- **Normalized Requirement**: TUI/view implementation shall be verified with the TUnit test framework.
- **Constraints**: Tests must run under trimming/AoT where feasible; prefer headless view construction/behavior tests over visual-only checks.
- **Cites**: D001

### [T005] - logging surface

- **Driver**: the user wants no change to the current error/diagnostic sink.
- **Resolved Answer**: "Logging stays as is."
- **Normalized Requirement**: The existing `FileLogger`/`FileLoggerProvider` shall remain the diagnostic sink; no new TUI log region is introduced.
- **Constraints**: The D003 feedback banner uses UI state, not the file logger.
- **Cites**: D003

### [T006] - glyph/config sourcing

- **Driver**: the user wants glyph and theme values sourced without reflection to preserve NativeAOT.
- **Resolved Answer**: "Pin Glyph sourcing"
- **Normalized Requirement**: Glyphs and theme values shall be read AoT-safely from `config.json` via const/record accessors only, with no runtime reflection.
- **Constraints**: No `dynamic` or reflection-based config binding; glyph set limited to widely-supported Unicode per D004.
- **Cites**: D004, T002

### [T007] - TrueColor theme scheme definition

- **Driver**: the user wants a modern neutral TrueColor palette filling all VisualRoles without a branded identity.
- **Resolved Answer**: "Option A — Inline 24-bit hex in config.json"
- **Normalized Requirement**: The CoAttribution theme in config.json shall define Base/Dialog/HotNormal/HotFocus/Active/ReadOnly/Disabled schemes using `#RRGGBB` 24-bit colors.
- **Constraints**: Must fill all VisualRoles per D002; no reflection-based binding (honors T006); graceful degradation handled in T010.
- **Cites**: D002, T002, T006

### [T008] - glyph vocabulary definition

- **Driver**: the user wants Unicode-only glyphs defined centrally and accessed without reflection.
- **Resolved Answer**: "Option A — Glyphs.* block + record accessor"
- **Normalized Requirement**: A `Glyphs` section shall be added to config.json and consumed via a record/const accessor with no runtime reflection.
- **Constraints**: Glyph set limited to widely-supported Unicode per D004; AoT-safe per T006.
- **Cites**: D004, T006, T002

### [T009] - dedicated feedback banner

- **Driver**: the user wants commit outcome communicated clearly without mutating Window.Title or adding clutter.
- **Resolved Answer**: "Option B — Transient toast overlay"
- **Normalized Requirement**: Commit success/failure/error shall be shown via a temporary overlay near the result, then auto-dismiss; `Window.Title` remains a stable identity string.
- **Constraints**: Must not mutate `Title` (D003); must not add a workflow step (D001); AoT-safe timer lifecycle.
- **Cites**: D003, D001, T005

### [T010] - graceful degradation strategy

- **Driver**: the user wants the modern theme to survive on terminals without truecolor.
- **Resolved Answer**: "Option A — Capability-gated swap"
- **Normalized Requirement**: At startup the TUI shall detect terminal truecolor support and automatically select a 16-color fallback scheme when truecolor is unavailable.
- **Constraints**: Must not regress on capable terminals (D002); fallback scheme must exist; avoid driver-API coupling that breaks AoT.
- **Cites**: D002, T007, T002

### [T011] - FrameView sectioning

- **Driver**: the user wants reusable, testable role-grouped sections for visual hierarchy.
- **Resolved Answer**: "Option B — Composite sub-views"
- **Normalized Requirement**: CommitForm (Subject/Body) and AuthorSelection (Identity/Selection/Attribution) sections shall be refactored into their own sub-view classes hosted inside labeled FrameViews.
- **Constraints**: Control behavior unchanged (D001); AoT-safe (T002); must coexist with T014 split-panel.
- **Cites**: D008, D001, T002

### [T012] - status-bar key glyphs

- **Driver**: the user wants clean alignment of key glyphs with their labels for discoverability.
- **Resolved Answer**: "Option B — Split glyph/text cells"
- **Normalized Requirement**: Status-bar entries shall render a dedicated glyph cell plus a text cell (via T008 glyphs), keeping `Key.Empty`/display-only behavior.
- **Constraints**: Real key bindings unchanged (D001/D005); AoT-safe; glyphs render on default terminals (D004).
- **Cites**: D005, D001, T008, T003

### [T013] - ListView selection migration

- **Driver**: the user wants a modern author list feel while preserving selection semantics.
- **Resolved Answer**: "Option A — Bound ListView + glyph"
- **Normalized Requirement**: The manual CheckBox stack in AuthorSelectionView shall be replaced by a Terminal.Gui ListView bound to a row source, showing selection via the T008 glyph; multi-select, filter, and advanced attribution cycling preserved.
- **Constraints**: Behavior unchanged (D001); AoT-safe (T002); integrates with T011 sub-views and T014 split-panel.
- **Cites**: D006, D001, T008, T011, T002

### [T014] - split-panel author layout

- **Driver**: the user wants a showcase split-panel aesthetic for author selection.
- **Resolved Answer**: "Option A — Two-pane FrameView"
- **Normalized Requirement**: AuthorSelectionView shall use a left filterable ListView pane and a right FrameView pane (selected/attribution) within the T011 sub-view structure, preserving the CommitForm→AuthorSelection→Preview flow.
- **Constraints**: Functionality preserved (D001/I001); focus management across panes; AoT-safe (T002).
- **Cites**: D007, D001, I001, T011, T013

### [T015] - layer boundaries

- **Driver**: the user wants to keep the existing CLI/Lib separation while adding presentation types.
- **Resolved Answer**: "Option A — Presentation types stay in Cli.Tui"
- **Normalized Requirement**: New presentation types (Glyphs accessor, sub-views, ListView row source, toast overlay) shall live in `CoAttribution.Cli.Tui`; `CoAttribution.Lib` shall remain free of Terminal.Gui references.
- **Constraints**: Lib holds reusable logic only (AGENTS.md); AoT-safe (T002); Cli.Tui may depend on Lib.Abstractions/Models.
- **Cites**: D001, T002, T003, T011, T013, T014

### [T016] - GlyphSet (glyph accessor record)

- **Driver**: the user wants glyphs sourced from config.json without reflection.
- **Resolved Answer**: "Option A — Record loaded from config.json"
- **Normalized Requirement**: A `GlyphSet` sealed record shall be parsed once from the config.json `Glyphs` section via MEC and exposed as a DI singleton.
- **Constraints**: No reflection (T006); glyph set limited to widely-supported Unicode (D004); lives in Cli.Tui (T015).
- **Cites**: T008, T006, D004, T015

### [T017] - FeedbackToast (transient overlay)

- **Driver**: the user wants a non-blocking transient commit-result overlay.
- **Resolved Answer**: "Option A — View + MainLoop timeout"
- **Normalized Requirement**: A `FeedbackToast : View` shall render above content and auto-dismiss via `Application.MainLoop.AddTimeout`; `Window.Title` unchanged.
- **Constraints**: Must not mutate Title (D003/T009); AoT-safe (T002); non-blocking (T009).
- **Cites**: T009, D003, T002, T003

### [T018] - AuthorListRow (ListView row source)

- **Driver**: the user wants a clean, testable row model for the ListView.
- **Resolved Answer**: "Option B — New AuthorListRow DTO"
- **Normalized Requirement**: A `AuthorListRow` sealed class shall carry Id/DisplayLabel/IsSelected/SelectedAttributionType/IsHostRow, mapped from the existing `AuthorRow`.
- **Constraints**: Preserves multi-select/filter/attribution (D001/T013); testable via TUnit (T004); in Cli.Tui (T015).
- **Cites**: T013, T015, T004, D001

### [T019] - CommitFormSectionsView (composite sub-view)

- **Driver**: the user wants CommitForm controls grouped into labeled FrameViews as sub-views.
- **Resolved Answer**: "Option A — Single composite View"
- **Normalized Requirement**: A `CommitFormSectionsView : View` shall host a `FrameView`(Subject) and `FrameView`(Body) wrapping the existing commit-form controls.
- **Constraints**: Control behavior unchanged (D001); AoT-safe (T002); uses T007 TrueColor schemes.
- **Cites**: T011, D001, T007, T015

### [T020] - AuthorSelectionPanelView (split-panel sub-view)

- **Driver**: the user wants the AuthorSelection split-panel as a composite sub-view.
- **Resolved Answer**: "Option A — Two-pane composite View"
- **Normalized Requirement**: A `AuthorSelectionPanelView : View` shall host a left filterable ListView FrameView (bound to AuthorListRow) and a right selected/attribution FrameView, preserving the commit flow.
- **Constraints**: Preserves flow (D001/I001); focus management across panes; AoT-safe (T002); integrates T013/T018.
- **Cites**: T014, D007, T011, T013, T018, D001, I001

<!-- next-d: D009 -->
<!-- next-t: T021 -->
<!-- next-i: I002 -->
