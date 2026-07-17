---
title: Host resolution implementations
classification: Independent
blocked_by: ["003-host-resolution-abstractions-and-result-types"]
parent: "Conversation context (2026-07-17) - Implementing per-host identity overrides in the attribution registry and the host-resolution precedence chain that selects which override renders for a given commit. Agreed on 4-step host precedence chain, strongly-typed host blocks, and source-generated validation. Out of scope - coattribution doctor subcommand, --save-host persistence, redundant-override linter."
---

## Goal

Implement the concrete host-resolution classes that fulfill the interfaces from TK003. This includes the 4-step precedence chain in `HostResolver`, the git config I/O client, and the remote URL probe.

## What to build

Three concrete classes under `src/CoAttribution.Lib/HostResolution/`:

1. `HostResolver` implementing `IHostResolver` - the core 4-step precedence chain (D003). Constructor-injected with `IGitConfigClient`, `IGitRemoteProbe`, and `AppConfig`. Must be `partial` but NOT `sealed` (open for unforeseen future use). The precedence chain is:
   - Step 1: If `hostInput` is non-null, validate via `HostKeyValidator.IsValid`; if valid, return `Resolved` with Source = `CliFlag`
   - Step 2: If `gitConfigClient.TryGet("coattribution.host")` succeeds, validate; if valid, return `Resolved` with Source = `GitConfig`
   - Step 3: If `gitRemoteProbe.GetPrimaryRemoteUrlAsync()` returns a URL, extract hostname, look up in `DefaultHostMap` then `AppConfig.HostAliases`; if found and valid, return `Resolved` with Source = `RemoteProbe`
   - Step 4: Return `NoHostDetected`
   Invalid values at any step fall through to the next step rather than throwing.

2. `GitConfigClient` implementing `IGitConfigClient` - shells out to `git config --get <key>` for `TryGet` and `git config <key> <value>` for `Set`. Must enforce that only `coattribution.*` keys are accepted in `Set` (throws `ArgumentException` otherwise). Uses the existing `CliInvoke` async command runner. Must be `partial`.

3. `GitRemoteProbe` implementing `IGitRemoteProbe` - shells out to `git remote -v`, prefers the first entry named `origin`, falls back to the first entry, returns null if output is empty or unparseable. Must be `partial`. Honours `CancellationToken`.

All three classes use constructor injection for testability with fakes.

## Size

- **Files** - 3 files to create
  - Create: `src/CoAttribution.Lib/HostResolution/HostResolver.cs`
  - Create: `src/CoAttribution.Lib/HostResolution/GitConfigClient.cs`
  - Create: `src/CoAttribution.Lib/HostResolution/GitRemoteProbe.cs`

## Recommended Workflow

### Step 1 — Create GitConfigClient

Where: `src/CoAttribution.Lib/HostResolution/GitConfigClient.cs`

- Create `public partial class GitConfigClient : IGitConfigClient` in namespace `CoAttribution.Lib.HostResolution`
- Define `private const string Namespace = "coattribution.";`
- Implement `TryGet` by shelling out to `git config --get <key>` - exit 0 with stdout output means value found; non-zero exit means key not found (return false)
- Implement `Set` with a guard that throws `ArgumentException` if the key does not start with `coattribution.`
- Use the existing `CliInvoke` infrastructure for async command execution with timeout
- Add MPL 2.0 license header

Verify: `TryGet("coattribution.host")` returns false when key is not set; `Set("evil.key", "x")` throws `ArgumentException`

### Step 2 — Create GitRemoteProbe

Where: `src/CoAttribution.Lib/HostResolution/GitRemoteProbe.cs`

- Create `public partial class GitRemoteProbe : IGitRemoteProbe` in namespace `CoAttribution.Lib.HostResolution`
- Implement `GetPrimaryRemoteUrlAsync` by shelling out to `git remote -v`
- Parse output to find the first entry named `origin`; fall back to the first entry; return null if empty or unparseable
- Honour the `CancellationToken` parameter
- Use the existing `CliInvoke` infrastructure

Verify: Returns null when no remotes are configured; returns the origin URL when origin exists

### Step 3 — Create HostResolver

Where: `src/CoAttribution.Lib/HostResolution/HostResolver.cs`

- Create `public partial class HostResolver : IHostResolver` in namespace `CoAttribution.Lib.HostResolution`
- Constructor takes `IGitConfigClient`, `IGitRemoteProbe`, `AppConfig`
- Implement the 4-step precedence chain:
  - Step 1: `hostInput` non-null -> validate with `HostKeyValidator.IsValid` -> if valid, return `Resolved` with `Source = HostSource.CliFlag`
  - Step 2: `gitConfigClient.TryGet("coattribution.host")` -> validate -> if valid, return `Resolved` with `Source = HostSource.GitConfig`
  - Step 3: `gitRemoteProbe.GetPrimaryRemoteUrlAsync()` -> extract hostname -> look up in `DefaultHostMap.Entries` then `AppConfig.HostAliases` -> validate -> if valid, return `Resolved` with `Source = HostSource.RemoteProbe`
  - Step 4: Return result with `Variant = HostResolutionVariant.NoHostDetected`
- Invalid values at any step fall through to the next step (do not throw)

Verify: With `hostInput = "github"`, returns `Resolved` with `Source = CliFlag`; with null input and no git config or remote, returns `NoHostDetected`

## Context pointers

**Files**
- `src/CoAttribution.Lib/HostResolution/Abstractions/IHostResolver.cs` - interface to implement (from TK003)
- `src/CoAttribution.Lib/HostResolution/Abstractions/IGitConfigClient.cs` - interface to implement (from TK003)
- `src/CoAttribution.Lib/HostResolution/Abstractions/IGitRemoteProbe.cs` - interface to implement (from TK003)
- `src/CoAttribution.Lib/HostResolution/HostKeyValidator.cs` - consumed for key validation (from TK002)
- `src/CoAttribution.Lib/HostResolution/DefaultHostMap.cs` - consumed for hostname lookup (from TK002)
- `src/CoAttribution.Cli/Models/AppConfig.cs` - consumed for HostAliases (from TK001)
- `src/CoAttribution.Lib/CliGitClient.cs` - reference for existing CliInvoke pattern

**ADRs** - None

**Domain terms**
- Precedence chain - the ordered sequence of sources consulted to determine the current host (CLI flag > git config > remote probe > fallback)
- Fall-through - when an invalid value at one precedence step is silently skipped in favour of the next step

**Ledger records**
- `DECISIONS-CoAttribution-agent-trailer-format.md#T003` - Location of host-resolution logic in CoAttribution.Lib
- `DECISIONS-CoAttribution-agent-trailer-format.md#T004` - I/O seam (git config and git remote, async timeout, CliInvoke usage)
- `DECISIONS-CoAttribution-agent-trailer-format.md#T005` - Resolver failure shape and fall-through behavior
- `DECISIONS-CoAttribution-agent-trailer-format.md#T008` - DefaultHostMap lookup in step 3
- `DECISIONS-CoAttribution-agent-trailer-format.md#T009` - Host-key validation at each precedence step
- `DECISIONS-CoAttribution-agent-trailer-format.md#T010` - AppConfig.HostAliases as source of truth for aliases
- `DECISIONS-CoAttribution-agent-trailer-format.md#T011` - --host is transient (Set never called from --host path)
- `DECISIONS-CoAttribution-agent-trailer-format.md#T016` - AppConfig.HostAliases is source of truth, not IGitConfigClient
- `DECISIONS-CoAttribution-agent-trailer-format.md#T019` - HostResolver is not sealed

## Acceptance criteria

- [ ] `HostResolver` implements `IHostResolver` and is `partial` but not `sealed`
- [ ] `HostResolver` constructor accepts `IGitConfigClient`, `IGitRemoteProbe`, and `AppConfig`
- [ ] The 4-step precedence chain is implemented in order (CliFlag, GitConfig, RemoteProbe, NoHostDetected)
- [ ] Invalid values at any step fall through to the next step without throwing
- [ ] `HostResolver` reads aliases from `AppConfig.HostAliases`, not from `IGitConfigClient`
- [ ] `GitConfigClient` implements `IGitConfigClient` and is `partial`
- [ ] `GitConfigClient.TryGet` shells out to `git config --get` and returns false on non-zero exit
- [ ] `GitConfigClient.Set` throws `ArgumentException` for non-`coattribution.*` keys
- [ ] `GitRemoteProbe` implements `IGitRemoteProbe` and is `partial`
- [ ] `GitRemoteProbe` prefers `origin` remote, falls back to first remote, returns null if none
- [ ] All I/O uses async command execution with timeout (no blocking waits)
- [ ] The solution builds without NativeAOT analyzer warnings

## Dependencies

**Blocked by** - `003-host-resolution-abstractions-and-result-types` (interfaces and result types to implement)
