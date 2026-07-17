---
title: Host resolution abstractions and result types
classification: Independent
blocked_by: ["001-toml-dto-shape", "002-host-key-validation-and-default-host-map"]
parent: "Conversation context (2026-07-17) - Implementing per-host identity overrides in the attribution registry and the host-resolution precedence chain that selects which override renders for a given commit. Agreed on 4-step host precedence chain, strongly-typed host blocks, and source-generated validation. Out of scope - coattribution doctor subcommand, --save-host persistence, redundant-override linter."
---

## Goal

Define the contract surface for host resolution - the interfaces, result types, enums, and diagnostic records that the resolver implementations and presentation layer will consume. This establishes the type-safe boundary between host-resolution logic and its callers.

## What to build

Six new types under `src/CoAttribution.Lib/HostResolution/` and its `Abstractions/` subfolder, all in namespace `CoAttribution.Lib.HostResolution` (or `.Abstractions` for interfaces):

1. `HostSource` enum with four values - `CliFlag`, `GitConfig`, `RemoteProbe`, `Fallback` - matching the D003 precedence chain top-to-bottom.

2. `HostResolutionResult` readonly record struct with three variants (`Resolved`, `MissingBlock`, `NoHostDetected`) carried via a `Variant` property of enum type `HostResolutionVariant`. Carries optional `Override`, `HostKey`, `Source`, and `ContributorId` properties depending on variant.

3. `HostResolutionVariant` enum with three values - `Resolved`, `MissingBlock`, `NoHostDetected`.

4. `IHostResolver` interface with a single method `HostResolutionResult ResolveHost(string? hostInput)` - source-agnostic parameter name so it works from CLI, TUI, and test fixtures.

5. `IGitConfigClient` interface with `bool TryGet(string key, [NotNullWhen(true)] out string? value)` and `void Set(string key, string value)`.

6. `IGitRemoteProbe` interface with `Task<string?> GetPrimaryRemoteUrlAsync(CancellationToken cancellationToken = default)`.

7. `MissingHostBlockDiagnostic` sealed record with four properties - `HostKey`, `ContributorId`, `RegistryPath`, `TomlSnippet`.

Interfaces live under `Abstractions/` per T017 placement rule.

## Size

- **Files** - 8 files to create
  - Create: `src/CoAttribution.Lib/HostResolution/HostSource.cs`
  - Create: `src/CoAttribution.Lib/HostResolution/HostResolutionVariant.cs`
  - Create: `src/CoAttribution.Lib/HostResolution/HostResolutionResult.cs`
  - Create: `src/CoAttribution.Lib/HostResolution/Abstractions/IHostResolver.cs`
  - Create: `src/CoAttribution.Lib/HostResolution/Abstractions/IGitConfigClient.cs`
  - Create: `src/CoAttribution.Lib/HostResolution/Abstractions/IGitRemoteProbe.cs`
  - Create: `src/CoAttribution.Lib/HostResolution/MissingHostBlockDiagnostic.cs`

## Recommended Workflow

### Step 1 — Create the Abstractions subdirectory

Where: `src/CoAttribution.Lib/HostResolution/Abstractions/`

- Create the `Abstractions` subdirectory under `src/CoAttribution.Lib/HostResolution/`

Verify: Directory exists

### Step 2 — Create HostSource and HostResolutionVariant enums

Where: `src/CoAttribution.Lib/HostResolution/HostSource.cs`, `src/CoAttribution.Lib/HostResolution/HostResolutionVariant.cs`

- Create `HostSource` enum with values: `CliFlag`, `GitConfig`, `RemoteProbe`, `Fallback`
- Create `HostResolutionVariant` enum with values: `Resolved`, `MissingBlock`, `NoHostDetected`
- Both in namespace `CoAttribution.Lib.HostResolution`

Verify: Enums compile and have the expected member count

### Step 3 — Create HostResolutionResult record struct

Where: `src/CoAttribution.Lib/HostResolution/HostResolutionResult.cs`

- Create a `public readonly record struct HostResolutionResult`
- Add properties: `HostResolutionVariant Variant`, `HostOverride? Override`, `string? HostKey`, `HostSource Source`, `string? ContributorId`
- All properties use `{ get; init; }` accessors

Verify: Record struct compiles and can be constructed with each variant

### Step 4 — Create the three interfaces

Where: `src/CoAttribution.Lib/HostResolution/Abstractions/IHostResolver.cs`, `IGitConfigClient.cs`, `IGitRemoteProbe.cs`

- Create `IHostResolver` with `HostResolutionResult ResolveHost(string? hostInput)`
- Create `IGitConfigClient` with `bool TryGet(string key, [NotNullWhen(true)] out string? value)` and `void Set(string key, string value)`
- Create `IGitRemoteProbe` with `Task<string?> GetPrimaryRemoteUrlAsync(CancellationToken cancellationToken = default)`
- All in namespace `CoAttribution.Lib.HostResolution.Abstractions`
- Add `using System.Diagnostics.CodeAnalysis;` for `[NotNullWhen]`

Verify: Interfaces compile; `[NotNullWhen(true)]` is recognized

### Step 5 — Create MissingHostBlockDiagnostic record

Where: `src/CoAttribution.Lib/HostResolution/MissingHostBlockDiagnostic.cs`

- Create a `public sealed record MissingHostBlockDiagnostic(string HostKey, string ContributorId, string RegistryPath, string TomlSnippet)`
- In namespace `CoAttribution.Lib.HostResolution`

Verify: Record compiles and all four properties are accessible

## Context pointers

**Files**
- `src/CoAttribution.Lib/Models/DTOs/HostOverride.cs` - referenced by `HostResolutionResult.Override` property (from TK001)
- `src/CoAttribution.Lib/Abstractions/IGitClient.cs` - existing interface pattern reference

**ADRs** - None

**Domain terms**
- Host resolution - the process of determining which git hosting platform the current commit targets
- Host resolution variant - the outcome category of host resolution (resolved, missing block, or no host detected)
- Missing host block - a resolved host where the contributor has no per-host identity override configured

**Ledger records**
- `DECISIONS-CoAttribution-agent-trailer-format.md#T003` - Location of host-resolution logic in CoAttribution.Lib
- `DECISIONS-CoAttribution-agent-trailer-format.md#T004` - I/O seam for .git/config and git remote (interface shapes, async timeout constraint)
- `DECISIONS-CoAttribution-agent-trailer-format.md#T005` - Resolver failure shape (three variants)
- `DECISIONS-CoAttribution-agent-trailer-format.md#T007` - CLI diagnostic structure (MissingHostBlockDiagnostic record)
- `DECISIONS-CoAttribution-agent-trailer-format.md#T014` - Physical separation of HostResolution types
- `DECISIONS-CoAttribution-agent-trailer-format.md#T015` - Source of truth for resolved host identity (IHostResolver only)
- `DECISIONS-CoAttribution-agent-trailer-format.md#T017` - Placement of HostResolution abstractions under Abstractions/ subfolder
- `DECISIONS-CoAttribution-agent-trailer-format.md#T018` - IHostResolver.ResolveHost parameter name (hostInput, not cliHost)

## Acceptance criteria

- [ ] `HostSource` enum has exactly four values in precedence order (CliFlag, GitConfig, RemoteProbe, Fallback)
- [ ] `HostResolutionVariant` enum has exactly three values (Resolved, MissingBlock, NoHostDetected)
- [ ] `HostResolutionResult` is a `readonly record struct` with Variant, Override, HostKey, Source, and ContributorId properties
- [ ] `IHostResolver` interface has `ResolveHost(string? hostInput)` returning `HostResolutionResult`
- [ ] `IGitConfigClient` interface has `TryGet` with `[NotNullWhen(true)]` and `Set` methods
- [ ] `IGitRemoteProbe` interface has `GetPrimaryRemoteUrlAsync` returning `Task<string?>`
- [ ] `MissingHostBlockDiagnostic` is a `sealed record` with HostKey, ContributorId, RegistryPath, TomlSnippet
- [ ] All interfaces live under `HostResolution/Abstractions/` in namespace `CoAttribution.Lib.HostResolution.Abstractions`
- [ ] The solution builds without NativeAOT analyzer warnings

## Dependencies

**Blocked by** - `001-toml-dto-shape` (HostOverride type referenced by HostResolutionResult), `002-host-key-validation-and-default-host-map` (HostKeyValidator referenced by resolver contract)
