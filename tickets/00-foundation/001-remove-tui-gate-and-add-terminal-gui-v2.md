---
title: Remove TUI gate, add Terminal.Gui v2 to csproj
classification: Independent
blocked_by: []
parent: docs/decisions/DECISIONS-CoAttribution-tui-mode.md
---

## Goal

Make Terminal.Gui v2 a permanent, un-gated dependency of the CLI so the TUI is part of the default NativeAOT build, removing the `#if TUI` MSBuild switch.

## What to build

Edit `src/CoAttribution.Cli/CoAttribution.Cli.csproj` to delete the `EnableTui` MSBuild property and its `TUI` define-constant block, leaving the Terminal.Gui PackageReference in place for the default build. Verify that `IsTrimmable=true` and `IsAoTCompatible=true` survive the change. After this ticket, Terminal.Gui v2 is compiled unconditionally, per D004 / T006.

## Size

- **Files** - 1 file edited

## Recommended Workflow

### Step 1 — Drop the TUI MSBuild gate

Where: `src/CoAttribution.Cli/CoAttribution.Cli.csproj`

- Remove the `<DefineConstants Condition="'$(EnableTui)' == 'true'">$(DefineConstants);TUI</DefineConstants>` line entirely.
- Confirm `Terminal.Gui` PackageReference stays in the project (no version gate added).

Verify: `dotnet build src/CoAttribution.Cli -p:EnableTui=false` succeeds and emits no `TUI`-conditional compile symbol anywhere in the build output.

### Step 2 — Verify AOT settings still hold

Where: `src/CoAttribution.Cli/CoAttribution.Cli.csproj`

- Confirm `<IsTrimmable>true</IsTrimmable>` and `<IsAoTCompatible>true</IsAoTCompatible>` remain set.

Verify: `dotnet build src/CoAttribution.Cli -c Release` produces no `IL2026`/`IL2067`/`IL2070`/`IL2072`/`IL3050` warnings introduced by removing the gate.

## Context pointers

**Files** - `src/CoAttribution.Cli/CoAttribution.Cli.csproj` is the only file this ticket edits; it currently carries the `EnableTui` define block.
**ADRs** - `docs/adr/0001-native-aot-constraint.md` defines the NativeAOT trim and analyzer requirements this build must continue to satisfy.
**Domain terms** - *Commit Trailer*, *Attribution* (from GLOSSARY.md) — relevant only because the TUI surfaces these in the commit flow.
**Ledger records** - `DECISIONS-CoAttribution-tui-mode.md#T006` — delete the gate and un-gate Terminal.Gui v2. `DECISIONS-CoAttribution-tui-mode.md#D004` — keep NativeAOT compatibility. `DECISIONS-CoAttribution-tui-mode.md#T001` — keep a single project structure.

## Acceptance criteria

- [ ] `EnableTui` MSBuild property and `TUI` define-constant block are removed from `src/CoAttribution.Cli/CoAttribution.Cli.csproj` (DECISIONS-CoAttribution-tui-mode.md#T006).
- [ ] `Terminal.Gui` PackageReference remains with no version gate so the default build pulls Terminal.Gui v2 (DECISIONS-CoAttribution-tui-mode.md#T006).
- [ ] `IsTrimmable=true` and `IsAoTCompatible=true` remain set (DECISIONS-CoAttribution-tui-mode.md#D004, DECISIONS-CoAttribution-tui-mode.md#T006).
- [ ] The project builds with `dotnet build src/CoAttribution.Cli -c Release` without introducing new AOT-trim analyzer warnings (DECISIONS-CoAttribution-tui-mode.md#D004).
- [ ] The solution remains a single .csproj for the UI surface; no new CLI sub-project is introduced (DECISIONS-CoAttribution-tui-mode.md#T001).

## Dependencies

**Blocked by** - None - can start immediately
