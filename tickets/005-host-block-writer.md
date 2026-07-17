---
title: Host block writer
classification: Independent
blocked_by: ["001-toml-dto-shape", "002-host-key-validation-and-default-host-map"]
parent: "Conversation context (2026-07-17) - Implementing per-host identity overrides in the attribution registry and the host-resolution precedence chain that selects which override renders for a given commit. Agreed on 4-step host precedence chain, strongly-typed host blocks, and source-generated validation. Out of scope - coattribution doctor subcommand, --save-host persistence, redundant-override linter."
---

## Goal

Create a pure data-transform class that adds a `host.<key>` block to a `GitCoAuthorConfig` without performing file I/O. This separates the data manipulation logic from the TOML serialization concern.

## What to build

A single class `HostBlockWriter` at `src/CoAttribution.Lib/HostResolution/HostBlockWriter.cs` in namespace `CoAttribution.Lib.HostResolution`.

The class has one method:

```csharp
public GitCoAuthorConfig Write(
    GitCoAuthorConfig config,
    string contributorId,
    string hostKey,
    HostOverride block)
```

This is a pure data transform - it takes a `GitCoAuthorConfig`, returns a new one (or mutates and returns the same one) with the `host.<key>` block added to the specified contributor. It does NOT perform file I/O; the actual TOML write is the caller's responsibility (an `IRegistryWriter` in `Cli`).

The `hostKey` parameter is taken as a separate `string` (not embedded in `HostOverride`) so that validation flows through `HostKeyValidator.IsValid` before the call. An invalid key throws because the caller violated the validation contract.

The class must be `partial` for AOT source-generation consistency.

## Size

- **Files** - 1 file to create
  - Create: `src/CoAttribution.Lib/HostResolution/HostBlockWriter.cs`

## Recommended Workflow

### Step 1 — Create HostBlockWriter class

Where: `src/CoAttribution.Lib/HostResolution/HostBlockWriter.cs`

- Create `public partial class HostBlockWriter` in namespace `CoAttribution.Lib.HostResolution`
- Implement the `Write` method with the signature above
- The method should locate the contributor by `contributorId` in either `config.Agents` or `config.Humans`
- Add the `HostOverride` block to the contributor's `Host` dictionary under the given `hostKey`
- Throw if the contributor is not found or if the `hostKey` is invalid (caller should have validated via `HostKeyValidator` before calling)
- Return the modified config
- Add MPL 2.0 license header

Verify: Given a config with a contributor "claude", calling `Write(config, "claude", "github", new HostOverride { Name = "Claude", Email = "claude@anthropic.com" })` adds the host block to the contributor

## Context pointers

**Files**
- `src/CoAttribution.Lib/Models/DTOs/GitCoAuthorConfig.cs` - the config type being transformed
- `src/CoAttribution.Lib/Models/GitCoAuthor.cs` - the contributor type with the Host dictionary (from TK001)
- `src/CoAttribution.Lib/Models/DTOs/HostOverride.cs` - the block type being added (from TK001)
- `src/CoAttribution.Lib/HostResolution/HostKeyValidator.cs` - validation contract the caller must satisfy (from TK002)

**ADRs** - None

**Domain terms**
- Host block - a per-host identity override entry in a contributor's Host dictionary
- Pure data transform - a function that manipulates in-memory data without performing I/O

**Ledger records**
- `DECISIONS-CoAttribution-agent-trailer-format.md#T001` - Foundation Lock (partial class for AOT)
- `DECISIONS-CoAttribution-agent-trailer-format.md#T006` - Layer boundaries (writer does not perform file I/O)
- `DECISIONS-CoAttribution-agent-trailer-format.md#T009` - Host-key validation contract (caller validates before calling)
- `DECISIONS-CoAttribution-agent-trailer-format.md#T012` - Layer boundaries for HostResolution feature

## Acceptance criteria

- [ ] `HostBlockWriter` exists as a `partial class` in namespace `CoAttribution.Lib.HostResolution`
- [ ] The `Write` method accepts `GitCoAuthorConfig`, `contributorId`, `hostKey`, and `HostOverride`
- [ ] The method adds the host block to the contributor's `Host` dictionary
- [ ] The method does NOT perform any file I/O (no TOML serialization or file writes)
- [ ] The method throws if the contributor is not found
- [ ] The solution builds without NativeAOT analyzer warnings

## Dependencies

**Blocked by** - `001-toml-dto-shape` (HostOverride and GitCoAuthor types), `002-host-key-validation-and-default-host-map` (HostKeyValidator contract)
