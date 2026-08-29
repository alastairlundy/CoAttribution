---
title: Confirm C# 14 / net10.0 and NativeAOT settings in csproj
classification: Independent
blocked_by: []
parent: docs/decisions/DECISIONS-CoAttribution-tui-modernization.md
---

## Goal

Ensure the CLI project targets `net10.0` with C# 14 and keeps its NativeAOT-compatible settings so the TUI modernization does not regress the AoT constraint (ADR 0001).

## What to build

Edit `src/CoAttribution.Cli/CoAttribution.Cli.csproj`. Confirm `TargetFramework` is `net10.0`, set `LangVersion` to C# 14 (or `latest`, which defaults to C# 14 on `net10.0`) if not already implied, and keep `IsTrimmable`, `IsAoTCompatible`, and the conditional `PublishAot` enabled. Do not downgrade the TFM and do not add Terminal.Gui package references beyond what T002 requires.

## Size

- **Files** - 1 (edit `CoAttribution.Cli.csproj`)

## Recommended Workflow

### Step 1 - Confirm framework and language settings

Where: `src/CoAttribution.Cli/CoAttribution.Cli.csproj`

- Confirm `<TargetFramework>net10.0</TargetFramework>` is present.
- Add or confirm `<LangVersion>` is at least C# 14 (use `latest` if implied by the TFM).

Verify: `dotnet build` reports `net10.0` and C# 14 language version.

### Step 2 - Verify AoT flags

Where: `src/CoAttribution.Cli/CoAttribution.Cli.csproj`

- Confirm `IsTrimmable=true`, `IsAoTCompatible=true`, and the `PublishAot` conditional remain enabled.

Verify: NativeAOT analyzer runs without new warnings; no TFM downgrade.

## Context pointers

##### Files

- `src/CoAttribution.Cli/CoAttribution.Cli.csproj` - the only file changed by this ticket.

##### ADRs

- `docs/adr/0001-native-aot-constraint.md` - NativeAOT compatibility is a hard constraint.

##### Ledger records

- `DECISIONS-CoAttribution-tui-modernization.md#T001` - C# 14 on .NET 10 LTS, NativeAOT.
- `DECISIONS-CoAttribution-tui-modernization.md#T002` - Terminal.Gui v2 stays the rendering engine.

## Acceptance criteria

- [ ] Project targets `net10.0` (T001)
- [ ] Language version is C# 14 and not downgraded (T001)
- [ ] `IsTrimmable`, `IsAoTCompatible`, and `PublishAot` remain enabled as before (T001, ADR 0001)
- [ ] No new Terminal.Gui package references are added that conflict with T002

## Dependencies

**Blocked by** - None - can start immediately
