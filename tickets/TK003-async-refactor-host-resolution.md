---
id: TK003
title: Async refactor of IGitConfigClient and IHostResolver
status: ready
Depends on: none
---

## Goal

Eliminate all sync-over-async patterns in `GitConfigClient` and `HostResolver` by making their interfaces fully async, following .NET best practices ("async all the way").

## What to build

1. **IGitConfigClient**: Change `bool TryGet(string key, out string? value)` to `Task<(bool Found, string? Value)> TryGetAsync(string key)`, and `void Set(string key, string value)` to `Task SetAsync(string key, string value)`. Remove the `[NotNullWhen(true)]` attribute (not applicable to tuple returns).

2. **GitConfigClient**: Replace `.GetAwaiter().GetResult()` with `await` in both methods. Remove the `BufferedProcessResult` blocking call.

3. **IHostResolver**: Change `HostResolutionResult ResolveHost(string? hostInput)` to `Task<HostResolutionResult> ResolveHostAsync(string? hostInput)`.

4. **HostResolver**: Make the method `async`, replace `.GetAwaiter().GetResult()` with `await` on `GetPrimaryRemoteUrlAsync`, and update the `TryGet` call to use the new `TryGetAsync` signature.

## Size

- **Files**: 4

## Recommended Workflow

### Step 1 — Update IGitConfigClient interface

Where: `src/CoAttribution.Lib/HostResolution/Abstractions/IGitConfigClient.cs`

- Change `bool TryGet(string key, [NotNullWhen(true)] out string? value)` to `Task<(bool Found, string? Value)> TryGetAsync(string key)`
- Change `void Set(string key, string value)` to `Task SetAsync(string key, string value)`
- Remove the `using System.Diagnostics.CodeAnalysis;` import if no longer needed

Verify: `dotnet build` reports errors in implementers and consumers (expected — next steps fix them)

### Step 2 — Update GitConfigClient implementation

Where: `src/CoAttribution.Lib/HostResolution/GitConfigClient.cs`

- Rename `TryGet` to `TryGetAsync` — change signature to `public async Task<(bool Found, string? Value)> TryGetAsync(string key)`
- Replace `.GetAwaiter().GetResult()` with `await` on `_processInvoker.ExecuteBufferedAsync(...)`
- Return `(true, result.StandardOutput.TrimEnd(...))` or `(false, null)` based on exit code
- Rename `Set` to `SetAsync` — change signature to `public async Task SetAsync(string key, string value)`
- Replace `.GetAwaiter().GetResult()` with `await` on `_processInvoker.ExecuteBufferedAsync(...)`

Verify: `dotnet build` passes

### Step 3 — Update IHostResolver interface

Where: `src/CoAttribution.Lib/HostResolution/Abstractions/IHostResolver.cs`

- Change `HostResolutionResult ResolveHost(string? hostInput)` to `Task<HostResolutionResult> ResolveHostAsync(string? hostInput)`

Verify: `dotnet build` reports errors in implementers (expected)

### Step 4 — Update HostResolver implementation

Where: `src/CoAttribution.Lib/HostResolution/HostResolver.cs`

- Change signature to `public async Task<HostResolutionResult> ResolveHostAsync(string? hostInput)`
- Await `_gitRemoteProbe.GetPrimaryRemoteUrlAsync(cancellationToken)` instead of `.GetAwaiter().GetResult()`
- Await `_gitConfigClient.TryGetAsync(...)` instead of calling `.TryGet(... out ...)` — deconstruct the tuple: `var (found, configuredHost) = await _gitConfigClient.TryGetAsync(...)`
- Pass a `CancellationToken` to `GetPrimaryRemoteUrlAsync` (introduce a `CancellationToken` parameter if needed, or use `default`)

Verify: `dotnet build` passes

## Context pointers

**Files**: `src/CoAttribution.Lib/HostResolution/Abstractions/IGitConfigClient.cs` — interface with out-parameter pattern; `src/CoAttribution.Lib/HostResolution/GitConfigClient.cs` — sync-over-async implementation; `src/CoAttribution.Lib/HostResolution/Abstractions/IHostResolver.cs` — sync interface; `src/CoAttribution.Lib/HostResolution/HostResolver.cs` — sync-over-async implementation.

**Ledger records**:
- `docs/decisions/DECISIONS-coattribution-audit-fixes.md#T005` — full async refactor of interfaces and implementations

## Acceptance criteria

- [ ] `IGitConfigClient` exposes `Task<(bool Found, string? Value)> TryGetAsync(string key)` and `Task SetAsync(string key, string value)`
- [ ] `GitConfigClient` contains no `.GetAwaiter().GetResult()` calls
- [ ] `IHostResolver` exposes `Task<HostResolutionResult> ResolveHostAsync(string? hostInput)`
- [ ] `HostResolver` contains no `.GetAwaiter().GetResult()` calls and uses `await` throughout
- [ ] `dotnet build` passes with 0 errors and 0 warnings
- [ ] NativeAOT compatibility is maintained (no reflection or dynamic code introduced)

## Dependencies

**Blocked by** - None - can start immediately
