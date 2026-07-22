---
id: TK004
title: Wire IHostResolver into CommitOrchestrator
status: ready
Depends on: TK003
---

## Goal

Inject `IHostResolver` into `CommitOrchestrator` so that host-aware default author IDs are automatically populated when building a commit message.

## What to build

1. Add `IHostResolver` as a constructor parameter to `CommitOrchestrator`.
2. In `BuildCommitMessageAsync`, call `ResolveHostAsync` to get the resolved host key.
3. Merge the resolved host key into the `defaultIds` set before calling `AttributionPolicy.Resolve`.

This ticket depends on TK003 completing first because `IHostResolver` and `IGitConfigClient` have async signatures that this code consumes.

## Size

- **Files**: 1

## Recommended Workflow

### Step 1 — Add IHostResolver to CommitOrchestrator constructor

Where: `src/CoAttribution.Lib/CommitOrchestrator.cs`

- Add `private readonly IHostResolver _hostResolver;` field
- Add `IHostResolver hostResolver` parameter to the constructor and store it
- Update the `Program.cs` DI registration if needed (deferred registration — verify `IHostResolver` and its dependencies are all registered)

Verify: `dotnet build` passes

### Step 2 — Call ResolveHostAsync in BuildCommitMessageAsync

Where: `src/CoAttribution.Lib/CommitOrchestrator.cs`

- In `BuildCommitMessageAsync`, before calling `AttributionPolicy.Resolve`, call `HostResolutionResult hostResult = await _hostResolver.ResolveHostAsync(cancellationToken);`
- If `hostResult.Variant == HostResolutionVariant.Resolved`, append `hostResult.HostKey` to the `defaultIds` set (or merge it into the array)
- Pass the merged IDs to `AttributionPolicy.Resolve`

Verify: `dotnet build` passes

## Context pointers

**Files**: `src/CoAttribution.Lib/CommitOrchestrator.cs` — orchestrator that builds and executes commit messages; `src/CoAttribution.Lib/HostResolution/Abstractions/IHostResolver.cs` — async interface from TK003.

**Ledger records**:
- `docs/decisions/DECISIONS-coattribution-audit-fixes.md#T007` — IHostResolver injected into CommitOrchestrator as Option A

## Acceptance criteria

- [ ] `CommitOrchestrator` accepts `IHostResolver` via constructor injection
- [ ] `BuildCommitMessageAsync` awaits `ResolveHostAsync` and merges the resolved host key into the default author IDs
- [ ] Both `CommitCommand` and `MessageCommand` automatically benefit from host-aware defaults (no changes to the command classes)
- [ ] `dotnet build` passes with 0 errors and 0 warnings
- [ ] NativeAOT compatibility is maintained

## Dependencies

**Blocked by** - TK003 (async refactor of IHostResolver and IGitConfigClient must complete first)
