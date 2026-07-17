---
title: TOML DTO shape for host overrides
classification: Independent
blocked_by: []
parent: "Conversation context (2026-07-17) - Implementing per-host identity overrides in the attribution registry and the host-resolution precedence chain that selects which override renders for a given commit. Agreed on 4-step host precedence chain, strongly-typed host blocks, and source-generated validation. Out of scope - coattribution doctor subcommand, --save-host persistence, redundant-override linter."
---

## Goal

Add the data transfer objects needed to represent per-host identity overrides in the TOML registry. This establishes the data shape that all downstream host-resolution logic consumes.

## What to build

Extend the existing TOML DTO layer with three additions:

1. A new `HostOverride` DTO at `src/CoAttribution.Lib/Models/DTOs/HostOverride.cs` with `Name` and `Email` string properties, both defaulted to empty strings. The class must be `partial` for AOT source-generation consistency.

2. A `Host` property on `GitCoAuthor` at `src/CoAttribution.Lib/Models/GitCoAuthor.cs` - a strongly-typed `Dictionary<string, HostOverride>` with `[TomlPropertyName("host")]` attribute. This allows each contributor to carry per-host identity overrides keyed by host identifier.

3. A `HostAliases` property on `AppConfig` at `src/CoAttribution.Cli/Models/AppConfig.cs` - a `Dictionary<string, string>` with `[TomlPropertyName("host_aliases")]` attribute. This is the user-level source of truth for custom host aliases, separate from the built-in default map.

The strongly-typed dictionary approach (not `Dictionary<string, Dictionary<string, string>>` or dynamic `TomlTable`) satisfies the NativeAOT analyzer requirements and gives the resolver a typed lookup.

## Size

- **Files** - 3 files to create or edit
  - Create: `src/CoAttribution.Lib/Models/DTOs/HostOverride.cs`
  - Edit: `src/CoAttribution.Lib/Models/GitCoAuthor.cs`
  - Edit: `src/CoAttribution.Cli/Models/AppConfig.cs`

## Recommended Workflow

### Step 1 — Create HostOverride DTO

Where: `src/CoAttribution.Lib/Models/DTOs/HostOverride.cs`

- Create a new file with namespace `CoAttribution.Lib.Models.DTOs`
- Define a `partial class HostOverride` with two auto-properties: `Name` (string, default empty) and `Email` (string, default empty)
- Add the standard MPL 2.0 license header matching existing files

Verify: File compiles and follows the same pattern as `GitCoAuthor.cs`

### Step 2 — Add Host property to GitCoAuthor

Where: `src/CoAttribution.Lib/Models/GitCoAuthor.cs`

- Add `using Tomlyn.Serialization;` if not already present
- Add a property: `[TomlPropertyName("host")] public Dictionary<string, HostOverride> Host { get; set; } = new();`
- Ensure the class is marked `partial` (it already is per the existing code)

Verify: `GitCoAuthor` deserializes a TOML file with a `[agents.foo.host.github]` block without errors

### Step 3 — Add HostAliases property to AppConfig

Where: `src/CoAttribution.Cli/Models/AppConfig.cs`

- Add a property: `[TomlPropertyName("host_aliases")] public Dictionary<string, string> HostAliases { get; set; } = new();`
- Place it alongside the other dictionary properties for consistency

Verify: `AppConfig` deserializes a TOML file with a `[host_aliases]` section without errors

## Context pointers

**Files**
- `src/CoAttribution.Lib/Models/GitCoAuthor.cs` - existing DTO to extend with Host property
- `src/CoAttribution.Cli/Models/AppConfig.cs` - existing config class to extend with HostAliases
- `src/CoAttribution.Lib/Models/DTOs/GitCoAuthorConfig.cs` - reference for DTO pattern and namespace

**ADRs** - None

**Domain terms**
- Host override - a per-host identity (Name/Email) that replaces the contributor's default identity when committing to that host
- Host alias - a user-defined mapping from a hostname to a host key

**Ledger records**
- `DECISIONS-CoAttribution-agent-trailer-format.md#T001` - Foundation Lock (NativeAOT compatibility, partial classes)
- `DECISIONS-CoAttribution-agent-trailer-format.md#T002` - TOML DTO shape for host blocks (strongly-typed dictionary, empty-string defaults)
- `DECISIONS-CoAttribution-agent-trailer-format.md#T010` - host_aliases schema in AppConfig (Dictionary<string, string>, validation at resolve time)

## Acceptance criteria

- [ ] `HostOverride` class exists with `Name` and `Email` string properties, both defaulted to empty strings
- [ ] `HostOverride` is marked `partial`
- [ ] `GitCoAuthor` has a `Host` property of type `Dictionary<string, HostOverride>` with `[TomlPropertyName("host")]`
- [ ] `AppConfig` has a `HostAliases` property of type `Dictionary<string, string>` with `[TomlPropertyName("host_aliases")]`
- [ ] All new properties are initialized to empty dictionaries to avoid null-reference issues
- [ ] The solution builds without NativeAOT analyzer warnings

## Dependencies

**Blocked by** - None - can start immediately
