# Implementation Blueprint: Agent Trailer Format

## Scope Binding

- **Linked Spec**: `AGENTS.md` (project charter — NativeAOT boundary, no index manipulation, CLI/Lib split, attribution-only scope) **and** `docs/decisions/DECISIONS-CoAttribution-agent-trailer-format.md` (D001–D006 functional decisions, treated as the feature spec per the spec-source resolution).
- **Decision Ledger**: `docs/decisions/DECISIONS-CoAttribution-agent-trailer-format.md` (D001–D006, T001–T020).

> **This blueprint is a context pointer valid ONLY for the linked spec above.** Do not apply it to other specifications or features without explicit authorization. Every technical statement in the body that satisfies a functional requirement cites a `Dxxx` or `Txxx` record using `filename#<Dxxx|Txxx>` format. The `## Ledger Reference` section at the bottom audits the binding in one pass.

## Overview

This blueprint implements per-host identity overrides in the `CoAttribution` attribution registry, plus the host-resolution precedence chain that selects which override (if any) renders for a given commit. The work spans the existing two-project structure: data + rules in `CoAttribution.Lib`, presentation in `CoAttribution.Cli`. The 4-step host precedence chain (D003) is the new orchestration entry point; the `host.<key>` override block (D001, D002) is the new per-contributor data shape.

## Foundation

The implementation runs on the existing C# / `net10.0` toolchain with `Tomlyn` for TOML, `Terminal.Gui` for the TUI, and `DotMake.CommandLine` for CLI parsing, inside the existing two-project layered structure [`T001`]. `IsTrimmable` + `IsAoTCompatible` on the CLI and `EnableAoTAnalyzer` on the Lib remain active; no new NuGet package is introduced for the per-host override blocks.

## Data Layer (`CoAttribution.Lib`)

### `HostOverride` (DTO)

Lives at `src/CoAttribution.Lib/Models/DTOs/HostOverride.cs` in namespace `CoAttribution.Lib.Models.DTOs`:

```csharp
public partial class HostOverride
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
```

- `partial` for AOT source-generation consistency [`T001`, `T002`].
- Defaulted to empty strings so the deserializer never has to null-check a host block [`T002`].
- Carries only `Name` and `Email` — no `url`, `display_name`, or other fields in this version (D001, D002) [`T002`].

### `Host` property on `GitCoAuthor`

`GitCoAuthor` is extended with:

```csharp
[TomlPropertyName("host")]
public Dictionary<string, HostOverride> Host { get; set; } = new();
```

- Strongly-typed dictionary — not `Dictionary<string, Dictionary<string, string>>` or dynamic `TomlTable` — so the AOT analyzer is satisfied and the resolver gets a typed lookup [`T002`].
- `GitCoAuthor` is made `partial` if it is not already [`T002`].
- Unrecognized host-block fields are silently dropped by the deserializer; a follow-up linter ticket warns on them (D001 forward risk) [`T002`].

### `AppConfig.HostAliases`

`AppConfig` gains one new property [`T010`]:

```csharp
[TomlPropertyName("host_aliases")]
public Dictionary<string, string> HostAliases { get; set; } = new();
```

- Matches the existing `Dictionary<string, string>` pattern at `src/CoAttribution.Lib/Models/AppConfig.cs:14-27` [`T010`].
- Per-user, not per-project — lives in the user's `AppConfig`, not in `.git/config` [`T010`].
- Validation happens at resolve time via `HostKeyValidator.TryValidate` (T009), not at deserialize time [`T010`].
- `AppConfig.HostAliases` is the **source of truth** for user-defined host aliases during a single invocation (D005, T016) [`T016`].

### `HostSource` (enum)

Lives at `src/CoAttribution.Lib/HostResolution/HostSource.cs` in namespace `CoAttribution.Lib.HostResolution`:

```csharp
public enum HostSource
{
    CliFlag,
    GitConfig,
    RemoteProbe,
    Fallback
}
```

- Four values match the D003 precedence chain top-to-bottom [`T005`].
- `Fallback` is reached when none of `hostInput`, `IGitConfigClient.TryGet("coattribution.host")`, or `IGitRemoteProbe.GetPrimaryRemoteUrlAsync()` yields a host (D003 step 4) [`T005`].

### `HostResolutionResult` (discriminated union)

Lives at `src/CoAttribution.Lib/HostResolution/HostResolutionResult.cs` in namespace `CoAttribution.Lib.HostResolution`:

```csharp
public readonly record struct HostResolutionResult
{
    public HostResolutionVariant Variant { get; init; }
    public HostOverride? Override { get; init; }      // Resolved only
    public string? HostKey { get; init; }             // Resolved, MissingBlock
    public HostSource Source { get; init; }           // Resolved only
    public string? ContributorId { get; init; }       // MissingBlock only
}
```

Three variants (T005 family carve-out, alphabetical):

- `MissingBlock` — the host was resolved, but the contributor has no `host.<hostKey>` block [`T005`, D004].
- `NoHostDetected` — no host could be derived from any precedence step [`T005`, D003].
- `Resolved` — host and override block are both available [`T005`].

`MissingBlock` and `NoHostDetected` are returned by callers after consulting `IHostResolver` — the resolver itself only answers "what host?" per T015 [`T015`].

### `DefaultHostMap` (static)

Lives at `src/CoAttribution.Lib/HostResolution/DefaultHostMap.cs` in namespace `CoAttribution.Lib.HostResolution`:

```csharp
public static class DefaultHostMap
{
    public static readonly IReadOnlyDictionary<string, string> Entries =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["github.com"] = "github",
            ["gitlab.com"] = "gitlab",
            ["bitbucket.org"] = "bitbucket",
            ["gitea.com"] = "gitea",        // corrected per T008 (was gitea.io in D005)
            ["codeberg.org"] = "codeberg",
        };
}
```

- Exactly five entries; test suite pins count and key/value pairs [`T008`, D005].
- `gitea.com` is the corrected canonical hostname (T008 supersedes D005's `gitea.io`) [`T008`].
- `StringComparer.OrdinalIgnoreCase` so `GitHub.com` and `github.com` resolve identically [`T008`].
- A startup log line fires when a user alias shadows a built-in mapping (D005) [`T008`].

### `HostKeyValidator` (static)

Lives at `src/CoAttribution.Lib/HostResolution/HostKeyValidator.cs` in namespace `CoAttribution.Lib.HostResolution`:

```csharp
public static partial class HostKeyValidator
{
    public const string Pattern = "^[a-z]+$";

    [GeneratedRegex(Pattern)]
    private static partial bool IsValidHostKey(string? value);

    public static bool IsValid(string? key) => IsValidHostKey(key);

    public static bool TryValidate(string? key, out string? error) { ... }
}
```

- Source-generated regex (T020) — bad pattern fails the build at compile time [`T020`].
- `Pattern` is a `const string` for documentation and test pinning [`T020`, `T009`].
- Hot-path: `IsValid` returns `bool` for use by `DefaultHostMap` test pins and `HostResolver` (T009) [`T009`].
- Diagnostic path: `TryValidate` returns a human-readable error naming the offending character — used by `MissingHostBlockDiagnostic` (T007) [`T009`, `T007`].

### `HostBlockWriter` (class)

Lives at `src/CoAttribution.Lib/HostResolution/HostBlockWriter.cs` in namespace `CoAttribution.Lib.HostResolution`:

```csharp
public partial class HostBlockWriter
{
    public GitCoAuthorConfig Write(
        GitCoAuthorConfig config,
        string contributorId,
        string hostKey,
        HostOverride block) { ... }
}
```

- Pure data transform — takes a `GitCoAuthorConfig`, returns a new one with the `host.<key>` block added [`T006`, `T012`].
- Does not perform file I/O; the actual TOML write is the caller's job (an `IRegistryWriter` in `Cli`, per T012) [`T012`].
- `hostKey` parameter is taken as a separate `string` (not embedded in `HostOverride`) so validation flows through `HostKeyValidator.IsValid` (T009) before the call; an invalid key throws because the caller violated the T009 contract [`T009`, `T012`].
- `partial` for AOT source-generation [`T001`].

### `MissingHostBlockDiagnostic` (record)

Lives at `src/CoAttribution.Lib/HostResolution/MissingHostBlockDiagnostic.cs` in namespace `CoAttribution.Lib.HostResolution`:

```csharp
public sealed record MissingHostBlockDiagnostic(
    string HostKey,
    string ContributorId,
    string RegistryPath,
    string TomlSnippet);
```

- Four typed properties map one-to-one to the D004 self-contained diagnostic fields [`T007`, D004].
- `sealed record` (not `record struct`) — heap-allocated once per failure, passed across the `Lib` → `Cli` boundary [`T007`].
- `TomlSnippet` is a pre-rendered string — the formatter does not call into `Tomlyn` at render time [`T007`].
- `RegistryPath` is the absolute path the user can edit (e.g., `~/.config/coattribution/AUTHORS.toml`); the caller computes it once and passes it in [`T007`].

## Abstractions (`CoAttribution.Lib/HostResolution/Abstractions/`)

Per T017, the three new interfaces live under `src/CoAttribution.Lib/HostResolution/Abstractions/` in namespace `CoAttribution.Lib.HostResolution.Abstractions` (not under the existing `src/CoAttribution.Lib/Abstractions/` folder) [`T017`].

### `IHostResolver`

Lives at `src/CoAttribution.Lib/HostResolution/Abstractions/IHostResolver.cs`:

```csharp
public interface IHostResolver
{
    /// <param name="hostInput">
    /// Optional, caller-supplied normalised host key that wins the precedence chain
    /// at the top of D003. May be null when the caller has no candidate to offer.
    /// Sources include: CLI flag, TUI selector, coattribution doctor (future), test fixture.
    /// </param>
    HostResolutionResult ResolveHost(string? hostInput);
}
```

- Source-agnostic parameter name `hostInput` (T018) — not `cliHost` — so the interface is reusable from CLI, TUI, future `coattribution doctor` (D003 follow-up), and test fixtures [`T018`, `T015`].
- The `Lib`-side source of truth for "what host am I on right now?" (T015) [`T015`].
- Concrete `HostResolver` consumes `IGitConfigClient`, `IGitRemoteProbe`, `AppConfig` internally; those dependencies do not leak into the interface signature [`T003`].

### `IGitConfigClient`

Lives at `src/CoAttribution.Lib/HostResolution/Abstractions/IGitConfigClient.cs`:

```csharp
public interface IGitConfigClient
{
    bool TryGet(string key, [NotNullWhen(true)] out string? value);
    void Set(string key, string value);  // deferred to T011 follow-up
}
```

- Two methods cover the `.git/config` I/O surface (T004) [`T004`].
- `Set` is declared now for completeness, but no caller in this version invokes it — `--host` is transient per T011; the `--save-host` write path is a follow-up decision [`T011`].
- `[NotNullWhen(true)]` is a NativeAOT-friendly nullability annotation, not a runtime check [`T004`].

### `IGitRemoteProbe`

Lives at `src/CoAttribution.Lib/HostResolution/Abstractions/IGitRemoteProbe.cs`:

```csharp
public interface IGitRemoteProbe
{
    Task<string?> GetPrimaryRemoteUrlAsync(CancellationToken cancellationToken = default);
}
```

- Single async method; `Task<string?>` aligns with the existing `CliInvoke` infrastructure and the T004 "no method shall block without an async timeout" constraint [`T004`].
- Returns `null` for "no remote configured" — a normal fall-through condition, not an exception (T005) [`T004`, `T005`].

## I/O Layer (`CoAttribution.Lib/HostResolution/`)

### `HostResolver` (concrete)

Lives at `src/CoAttribution.Lib/HostResolution/HostResolver.cs`:

```csharp
public partial class HostResolver : IHostResolver
{
    public HostResolver(
        IGitConfigClient gitConfigClient,
        IGitRemoteProbe gitRemoteProbe,
        AppConfig appConfig) { ... }

    public HostResolutionResult ResolveHost(string? hostInput) { ... }
}
```

- `partial` for AOT source generation [`T001`]; **not** `sealed` per T019 — open for unforeseen future use [`T019`].
- Constructor-injected dependencies keep the class testable with fakes for all three seams [`T003`].
- Implements the 4-step precedence chain (D003): [`T003`]
  1. `hostInput` non-null? Validate via `HostKeyValidator.IsValid`; return `Resolved` (Source = `CliFlag`) [`T009`].
  2. `gitConfigClient.TryGet("coattribution.host")`? Validate; return `Resolved` (Source = `GitConfig`) [`T009`].
  3. `gitRemoteProbe.GetPrimaryRemoteUrlAsync()` → hostname → `DefaultHostMap ∪ AppConfig.HostAliases`? Validate; return `Resolved` (Source = `RemoteProbe`) [`T008`, `T010`, `T016`].
  4. Return `NoHostDetected` (D003 step 4) [`T005`].
- An invalid `hostInput` or `.git/config` value falls through to the next step rather than throwing [`T005`, `T009`].
- `AppConfig.HostAliases` is the source of truth — the resolver never reads aliases from `IGitConfigClient` (T016) [`T016`].
- The resolver itself only answers "what host?" — `MissingBlock` and `NoHostDetected` are produced here but the per-contributor lookup is the caller's job (T015) [`T015`].

### `GitConfigClient` (concrete)

Lives at `src/CoAttribution.Lib/HostResolution/GitConfigClient.cs`:

```csharp
public partial class GitConfigClient : IGitConfigClient
{
    private const string Namespace = "coattribution.";

    public bool TryGet(string key, [NotNullWhen(true)] out string? value) { ... }
    public void Set(string key, string value) { ... }
}
```

- `TryGet` shells out to `git config --get <key>`; exit 0 + stdout → value; exit non-zero → `false` (key not found) [`T004`].
- `Set` enforces the T004 constraint at runtime — only `coattribution.*` keys are accepted; the `ArgumentException` on a non-`coattribution.*` key is a genuine misuse signal (caller violated the T004 contract), not control flow [`T004`].
- `partial` for AOT source generation [`T001`].
- No `Process.WaitForExit` without timeout; uses `CliInvoke`'s async command runner [`T004`].

### `GitRemoteProbe` (concrete)

Lives at `src/CoAttribution.Lib/HostResolution/GitRemoteProbe.cs`:

```csharp
public partial class GitRemoteProbe : IGitRemoteProbe
{
    public async Task<string?> GetPrimaryRemoteUrlAsync(CancellationToken cancellationToken = default) { ... }
}
```

- Shells out to `git remote -v`; prefers the first entry whose name is `origin`; falls back to the first entry; returns `null` if output is empty or unparseable [`T004`].
- The "primary" rule (origin first, then first remote) is enforced here, not in `HostResolver` — the I/O class owns the I/O semantics [`T004`].
- Honours the `CancellationToken`; no `Process.WaitForExit` without timeout (T004) [`T004`].

## Presentation Layer (`CoAttribution.Cli`)

### `MissingHostBlockChoice` (enum)

Lives at `src/CoAttribution.Cli/Components/Dialogs/MissingHostBlockChoice.cs`:

```csharp
public enum MissingHostBlockChoice
{
    Add,
    SwitchHost,
    UseFallback
}
```

- Three values for the three D004 dialog actions (T006) [`T006`, D004].
- Order matches the button order in the dialog (left-to-right) so a future `default:` branch in a `switch` is stable [`T006`].

### `MissingHostBlockDialog` (class)

Lives at `src/CoAttribution.Cli/Components/Dialogs/MissingHostBlockDialog.cs`:

```csharp
public sealed class MissingHostBlockDialog : Dialog
{
    public MissingHostBlockChoice Choice => _choice;

    public MissingHostBlockDialog(MissingBlock missingBlock) { ... }
}
```

- Dedicated `Dialog` subclass — not a reuse of `AddAuthorDialog` (different domain) [`T006`].
- `sealed` because the three actions are a fixed set (D004); **not** `partial` — matches the existing `AddAuthorDialog`/`SetupDialog` convention [`T006`].
- Accepts the `MissingBlock(string HostKey, string ContributorId)` variant of `HostResolutionResult` (T005) [`T005`].
- Three buttons: "Add block" → `Add`, "Switch host" → `SwitchHost`, "Use fallback" → `UseFallback` (D004 wording exact) [`T006`, D004].
- Does not perform the registry write itself; the caller dispatches on `Choice` and calls `HostBlockWriter` (T006, T012) [`T006`, `T012`].

### `MissingHostBlockDiagnosticFormatter` (class)

Lives at `src/CoAttribution.Cli/HostResolution/MissingHostBlockDiagnosticFormatter.cs`:

```csharp
public sealed class MissingHostBlockDiagnosticFormatter
{
    public string Format(MissingHostBlockDiagnostic diagnostic) { ... }
}
```

- Thin, reviewable shell around the `MissingHostBlockDiagnostic` record (T007, T013) [`T007`].
- Renders a localized, multi-line string from `Resources.resx` with four substitution slots: `{HostKey}`, `{ContributorId}`, `{RegistryPath}`, `{TomlSnippet}` [`T007`, D004].
- `sealed` — single rendering strategy, no inheritance use case [`T007`].
- Lives in `Cli/HostResolution/` (not in `Cli/Components/Dialogs/`) — the formatter is consumed by the CLI command path, not the TUI dialog path [`T007`].
- Does not call into `Tomlyn` — the `TomlSnippet` arrives pre-rendered [`T007`].

## Architectural Rules (Enforced by Test Suite + Future Lint)

1. **Strict one-way dependency**: `CoAttribution.Lib` has zero project references to `CoAttribution.Cli` [`T013`]. A test asserts the compiled `Lib` assembly has zero references to the `Cli` assembly [`T013`].
2. **Source of truth for resolved host identity**: `IHostResolver` only — `AuthorRegistry` does not gain a `ResolveHost` method or `ResolvedHost` property [`T015`].
3. **Source of truth for `coattribution.host` value**: `AppConfig.HostAliases` (in-memory); `IGitConfigClient` is a read cache for `.git/config` keys [`T016`].
4. **Host-key validation**: `HostKeyValidator` is the only type permitted to validate host keys; a follow-up lint rule flags any other call site that hand-rolls the check [`T009`].
5. **`host.<key>` path construction**: `HostResolver` is the only type that constructs a `host.<key>` path; the lint rule from `HostKeyValidator` extends to this constraint [`T009`, `T015`].
6. **`--host` is transient**: `IGitConfigClient.Set` is never called from the `--host` code path; persistence requires an explicit, separate user action (T011 follow-up) [`T011`].
7. **No index manipulation**: `IGitConfigClient.Set` writes only to `.git/config` keys under the `coattribution.*` namespace; never calls `git add` or any other index-mutating command (AGENTS.md "No Index Manipulation") [`T004`].

## Deferred Work (Filed as Follow-up Tickets)

The following items are explicitly out of scope for this version and shall be filed as follow-up tickets:

- **`coattribution doctor` subcommand** (D003 follow-up) — surface the resolved host + `HostSource` for inspection.
- **Persistence surface for `coattribution.host`** (T011 follow-up) — `--save-host` flag vs. `coattribution host set` subcommand vs. `coattribution doctor` write action. New ADR record before implementation.
- **Redundant-override linter** (D001, D002 forward risk) — warn when a `host.<key>` block exactly duplicates the top-level `name`/`email` fallback.
- **Dxxx record to supersede D005 with the `gitea.com` correction** (T008) — the durable functional record still says `gitea.io`; a new Dxxx shall supersede it with `gitea.com` so the functional record reflects the corrected canonical hostname.

## Ledger Reference

Every Dxxx and Txxx record cited in this blueprint, in order:

- `D001` — shape of per-host identity in the registry
- `D002` — simplifying the registry structure
- `D003` — how the orchestrator knows which host block to render
- `D004` — behavior when the resolved host has no `host.<key>` block
- `D005` — how a `git remote` URL maps to a host key
- `D006` — host key naming convention
- `T001` — Foundation Lock
- `T002` — TOML DTO shape for `host.<key>` blocks
- `T003` — Location of host-resolution logic in `CoAttribution.Lib`
- `T004` — I/O seam for `.git/config` and `git remote -v`
- `T005` — Resolver failure shape: how `IHostResolver` signals a missing `host.<key>` block
- `T006` — TUI dialog structure for a missing `host.<key>` block
- `T007` — CLI diagnostic structure for a missing `host.<key>` block
- `T008` — Representation of the default `hostname → hostKey` map
- `T009` — Host-key validation rule
- `T010` — `host_aliases` schema in `AppConfig`
- `T011` — `coattribution.host` persistence
- `T012` — Layer boundaries for the new `HostResolution` feature
- `T013` — Dependency direction across `Lib` and `Cli`
- `T014` — Physical separation of the new `HostResolution` types
- `T015` — Source of truth for resolved host identity
- `T016` — Source of truth for `coattribution.host` value
- `T017` — Placement of `HostResolution` abstractions (interfaces)
- `T018` — `IHostResolver.ResolveHost` parameter name
- `T019` — `HostResolver` is not `sealed`
- `T020` — `HostKeyValidator` uses `[GeneratedRegex]` source generator
