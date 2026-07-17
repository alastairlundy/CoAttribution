---
title: Presentation layer for missing host blocks
classification: Independent
blocked_by: ["003-host-resolution-abstractions-and-result-types"]
parent: "Conversation context (2026-07-17) - Implementing per-host identity overrides in the attribution registry and the host-resolution precedence chain that selects which override renders for a given commit. Agreed on 4-step host precedence chain, strongly-typed host blocks, and source-generated validation. Out of scope - coattribution doctor subcommand, --save-host persistence, redundant-override linter."
---

## Goal

Build the presentation-layer types for handling missing host blocks - a TUI dialog for interactive use and a CLI diagnostic formatter for non-interactive use. These present the three D004 dialog actions to the user when a resolved host has no corresponding `host.<key>` block.

## What to build

Three types split across the TUI dialog path and the CLI command path:

1. `MissingHostBlockChoice` enum at `src/CoAttribution.Cli/Components/Dialogs/MissingHostBlockChoice.cs` with three values - `Add`, `SwitchHost`, `UseFallback` - matching the D004 dialog button order (left-to-right).

2. `MissingHostBlockDialog` sealed class at `src/CoAttribution.Cli/Components/Dialogs/MissingHostBlockDialog.cs` extending `Dialog`. Accepts a `HostResolutionResult` in the `MissingBlock` variant state. Exposes a `Choice` property of type `MissingHostBlockChoice`. Three buttons with exact D004 wording - "Add block" -> `Add`, "Switch host" -> `SwitchHost`, "Use fallback" -> `UseFallback`. Does NOT perform the registry write itself; the caller dispatches on `Choice`. Must be `sealed` (fixed action set) but NOT `partial` (matches existing dialog convention).

3. `MissingHostBlockDiagnosticFormatter` sealed class at `src/CoAttribution.Cli/HostResolution/MissingHostBlockDiagnosticFormatter.cs` with a `Format(MissingHostBlockDiagnostic diagnostic)` method. Renders a localized, multi-line string from `Resources.resx` with four substitution slots (`{HostKey}`, `{ContributorId}`, `{RegistryPath}`, `{TomlSnippet}`). Does NOT call into `Tomlyn` - the `TomlSnippet` arrives pre-rendered. Lives in `Cli/HostResolution/` (not in `Cli/Components/Dialogs/`) because it is consumed by the CLI command path, not the TUI dialog path.

## Size

- **Files** - 3 files to create
  - Create: `src/CoAttribution.Cli/Components/Dialogs/MissingHostBlockChoice.cs`
  - Create: `src/CoAttribution.Cli/Components/Dialogs/MissingHostBlockDialog.cs`
  - Create: `src/CoAttribution.Cli/HostResolution/MissingHostBlockDiagnosticFormatter.cs`

## Recommended Workflow

### Step 1 — Create MissingHostBlockChoice enum

Where: `src/CoAttribution.Cli/Components/Dialogs/MissingHostBlockChoice.cs`

- Create `public enum MissingHostBlockChoice` with values `Add`, `SwitchHost`, `UseFallback` in that order
- Add MPL 2.0 license header

Verify: Enum has exactly three values in the expected order

### Step 2 — Create MissingHostBlockDialog

Where: `src/CoAttribution.Cli/Components/Dialogs/MissingHostBlockDialog.cs`

- Create `public sealed class MissingHostBlockDialog : Dialog` (note - not `partial`, matching existing `AddAuthorDialog`/`SetupDialog` convention)
- Accept a `HostResolutionResult` in the constructor (the caller ensures it is in the `MissingBlock` variant)
- Add three buttons with D004 exact wording: "Add block", "Switch host", "Use fallback"
- Expose a `Choice` property of type `MissingHostBlockChoice` that is set when a button is pressed
- Follow the existing dialog patterns from `AddAuthorDialog.cs` and `SetupDialog.cs`

Verify: Dialog renders three buttons; pressing each sets the `Choice` property to the expected enum value

### Step 3 — Create MissingHostBlockDiagnosticFormatter

Where: `src/CoAttribution.Cli/HostResolution/MissingHostBlockDiagnosticFormatter.cs`

- Create the `HostResolution` directory under `src/CoAttribution.Cli/` if it does not exist
- Create `public sealed class MissingHostBlockDiagnosticFormatter` with a `Format(MissingHostBlockDiagnostic diagnostic)` method
- The method renders a multi-line string using `Resources.resx` with four substitution slots
- Does NOT call into `Tomlyn` - the `TomlSnippet` is used as-is from the diagnostic record

Verify: `Format` produces a readable multi-line string with all four fields substituted

## Context pointers

**Files**
- `src/CoAttribution.Cli/Components/Dialogs/AddAuthorDialog.cs` - existing dialog pattern reference
- `src/CoAttribution.Cli/Components/Dialogs/SetupDialog.cs` - existing dialog pattern reference
- `src/CoAttribution.Lib/HostResolution/MissingHostBlockDiagnostic.cs` - the record consumed by the formatter (from TK003)
- `src/CoAttribution.Lib/HostResolution/HostResolutionResult.cs` - the result type consumed by the dialog (from TK003)

**ADRs** - None

**Domain terms**
- Missing host block - when the host was resolved but the contributor has no per-host identity override for that host
- Diagnostic - a self-contained, human-readable explanation with enough context for the user to fix the issue

**Ledger records**
- `DECISIONS-CoAttribution-agent-trailer-format.md#T005` - HostResolutionResult MissingBlock variant consumed by the dialog
- `DECISIONS-CoAttribution-agent-trailer-format.md#T006` - TUI dialog structure (three buttons, exact D004 wording, sealed not partial)
- `DECISIONS-CoAttribution-agent-trailer-format.md#T007` - CLI diagnostic structure (formatter, Resources.resx, no Tomlyn call)
- `DECISIONS-CoAttribution-agent-trailer-format.md#T012` - Layer boundaries (dialog does not perform registry write)
- `DECISIONS-CoAttribution-agent-trailer-format.md#T013` - Dependency direction (Cli depends on Lib, not vice versa)

## Acceptance criteria

- [ ] `MissingHostBlockChoice` enum has exactly three values (Add, SwitchHost, UseFallback) in button order
- [ ] `MissingHostBlockDialog` is `sealed` but not `partial`, extending `Dialog`
- [ ] `MissingHostBlockDialog` accepts a `HostResolutionResult` in the constructor
- [ ] `MissingHostBlockDialog` exposes a `Choice` property of type `MissingHostBlockChoice`
- [ ] Button labels match D004 exact wording ("Add block", "Switch host", "Use fallback")
- [ ] `MissingHostBlockDialog` does NOT perform any registry write
- [ ] `MissingHostBlockDiagnosticFormatter` is `sealed` with a `Format` method
- [ ] `Format` uses `Resources.resx` for localization with four substitution slots
- [ ] `Format` does NOT call into `Tomlyn`
- [ ] `MissingHostBlockDiagnosticFormatter` lives in `Cli/HostResolution/` (not in `Cli/Components/Dialogs/`)
- [ ] The solution builds without NativeAOT analyzer warnings

## Dependencies

**Blocked by** - `003-host-resolution-abstractions-and-result-types` (HostResolutionResult and MissingHostBlockDiagnostic types)
