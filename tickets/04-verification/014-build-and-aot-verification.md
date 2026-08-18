---
title: Build and AOT verification (gate the merge)
classification: Independent
blocked_by: [001-remove-tui-gate-and-add-terminal-gui-v2, 003-tui-dispatch-tty-check-and-setupdialog-trigger-on-rootcommand, 005-tui-composition-root-and-status-bar-infrastructure, 009-setupdialog]
parent: docs/decisions/DECISIONS-CoAttribution-tui-mode.md
---

## Goal

Prove that the TUI lands without regressing NativeAOT compatibility and without regressing the non-TTY exit behavior the CLI relies on.

## What to build

Run, capture, and verify three checks against `src/CoAttribution.Cli` after all the other tickets in this PR have landed:

1. `dotnet build src/CoAttribution.Cli -c Release` succeeds with no `IL2026`, `IL2067`, `IL2070`, `IL2072`, `IL3050` warnings introduced by the TUI changes.
2. The binary still prints help and exits `0` when either `Console.IsOutputRedirected` or `Console.IsInputRedirected` is true (no TUI launch, no commit attempt).
3. The empty-registry path actually enters `SetupDialog` first (so the v2 wiring round-trips with the dialog in place).

This ticket is verification only — it does not change production code.

## Size

No files are added, edited, or deleted by this ticket.

## Recommended Workflow

### Step 1 — Run the AOT-clean Release build

Where: repo root

- Run `dotnet build src/CoAttribution.Cli -c Release` and capture the full log.
- Search for `IL2026`, `IL2067`, `IL2070`, `IL2072`, `IL3050`. Any match in a file under `src/CoAttribution.Cli/Tui/` opened by this PR is a regression to fix before merging.

Verify: the build exits 0; the warning filter returns no matches from new TUI files. (`docs/adr/0001-native-aot-constraint.md` defines the constraint; this step is the executable check.)

### Step 2 — Verify the non-TTY exit-0 behavior

Where: repo root

- Run `echo "" | dotnet run --project src/CoAttribution.Cli --` (no subcommand) in a shell that has no TTY for stdin or stdout.
- Confirm the process exits `0` and emits the help text.

Verify: `LASTEXITCODE` (or `$?`) is `0`; the captured stdout contains the DotMake help output and no Terminal.Gui-driven artifact.

### Step 3 — Verify the empty-registry path enters SetupDialog first

Where: repo root, with an author registry that contains zero entries

- Point `AppConfigRegistryPathResolver` at a fresh empty registry (e.g. an isolated `config-file` via `--config-path`).
- Run `dotnet run --project src/CoAttribution.Cli --` in a real TTY.
- Confirm the TUI lands on `SetupDialog`, not `MainWindow`.

Verify: the first interactive screen the user sees is `SetupDialog` (TK009); `MainWindow` is reached only after the user adds a first author.

## Context pointers

**Files** - none; this ticket is verification only.
**ADRs** - `docs/adr/0001-native-aot-constraint.md` — defines the trim/AOT rules this ticket enforces at build time.
**Domain terms** - *Attribution*, *Commit Trailer* — surface only insofar as the build / runtime checks confirm the orchestrator still loads cleanly.
**Ledger records** - `DECISIONS-CoAttribution-tui-mode.md#D004` — NativeAOT compatibility must continue to hold. `DECISIONS-CoAttribution-tui-mode.md#T006` — single Configuration build with no MSBuild gate. `DECISIONS-CoAttribution-tui-mode.md#T005` — TTY detection. `DECISIONS-CoAttribution-tui-mode.md#D005` — non-TTY exits 0 with help. `DECISIONS-CoAttribution-tui-mode.md#D007` — `SetupDialog` is the empty-registry gate. `DECISIONS-CoAttribution-tui-mode.md#T016` — `IAuthorRegistry.Count == 0` triggers `SetupDialog` before `MainWindow`.

## Acceptance criteria

- [ ] `dotnet build src/CoAttribution.Cli -c Release` exits `0` with no `IL2026` / `IL2067` / `IL2070` / `IL2072` / `IL3050` warnings from files added by this PR (DECISIONS-CoAttribution-tui-mode.md#D004, DECISIONS-CoAttribution-tui-mode.md#T006).
- [ ] `echo "" | dotnet run --project src/CoAttribution.Cli --` exits `0` and prints help text, never entering the TUI (DECISIONS-CoAttribution-tui-mode.md#T005, DECISIONS-CoAttribution-tui-mode.md#D005).
- [ ] With an empty author registry and a real TTY, `co-attr` (no args) enters `SetupDialog` before `MainWindow` (DECISIONS-CoAttribution-tui-mode.md#D007, DECISIONS-CoAttribution-tui-mode.md#T016).
- [ ] No `#if TUI` MSBuild property is reintroduced after this PR; the build succeeds with a single configuration (DECISIONS-CoAttribution-tui-mode.md#T006).
- [ ] Verification results and log snippets are recorded in the PR description (DECISIONS-CoAttribution-tui-mode.md#D020 — session-goal handoff to implementation).

## Dependencies

**Blocked by** - 001-remove-tui-gate-and-add-terminal-gui-v2, 003-tui-dispatch-tty-check-and-setupdialog-trigger-on-rootcommand, 005-tui-composition-root-and-status-bar-infrastructure, 009-setupdialog
