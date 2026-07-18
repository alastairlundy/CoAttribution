# Error Handoff: Pre-Existing Build Errors

> **Status**: Out of scope for the host-override work. Surfaced as a side-effect of the `ConfigSettingsTomlContext` layering fix on 2026-07-17. A future agent should pick this up in a dedicated ticket.
>
> **Verification command**: `dotnet build src/CoAttribution.slnx` from the repo root.
> **Current count**: 6 errors, 0 warnings, target framework `net10.0`.

> **Note (2026-07-18, host-override work)**: As part of tickets 3/4/6, `AppConfig` was moved from `CoAttribution.Cli.Models` → `CoAttribution.Lib.Models` to satisfy the T013 layering rule (`HostResolver` lives in `Lib` and needs to consume `AppConfig`). The `global using CoAttribution.Cli.Models;` line was removed from `src/CoAttribution.Cli/GlobalUsings.cs`; explicit `using CoAttribution.Cli.Models;` directives in `src/CoAttribution.Cli/Abstractions/IConfigResolver.cs` and `src/CoAttribution.Cli/DataAccess/ConfigSettingsTomlContext.cs` were retargeted to `CoAttribution.Lib.Models`. All other consumers resolve `AppConfig` through the existing `global using CoAttribution.Lib.Models;` in `src/CoAttribution.Cli/GlobalUsings.cs`. The `src/CoAttribution.Cli/Models/` folder is now empty and can be deleted by a future agent if desired.

## Why these errors are visible now

The `CoAttribution.Cli` build was previously masked by a generator failure in `src/CoAttribution.Lib/DataAccess/ConfigSettingsTomlContext.cs`, which `[TomlSerializable(typeof(AppConfig))]`-ed a `Cli` type from inside `Lib`. The Tomlyn source generator emitted broken type info into `obj/.../Lib/.../generated/.../`, so the Lib csproj failed to compile and the rest of the solution never got a chance to fail.

When that layering bug was fixed (the context was moved to `src/CoAttribution.Cli/DataAccess/ConfigSettingsTomlContext.cs`, namespace `CoAttribution.Cli.DataAccess`, with a direct `Tomlyn` `PackageReference` on the CLI csproj and `global using CoAttribution.Cli.DataAccess;` plus `global using CoAttribution.Cli.Models;` in `src/CoAttribution.Cli/GlobalUsings.cs`), the Lib project compiled. The CLI project then started compiling and surfaced its own latent issues that had been hidden behind the Lib failure.

None of the files below were touched by the layering fix or by any host-override ticket. They were broken before the layering fix and remain broken after it.

## Error inventory

### E1 — `FileHelper.ResolveExistingConfigFile` does not exist

- **File**: `src/CoAttribution.Cli/Commands/ConfigCommand.cs:42`
- **Error**: `error CS0117: 'FileHelper' does not contain a definition for 'ResolveExistingConfigFile'`
- **Call site**:
  ```csharp
  ConfigPath = FileHelper.ResolveExistingConfigFile(_configuration).FullName;
  ```
- **What `FileHelper` actually exposes** (`src/CoAttribution.Cli/Helpers/FileHelper.cs`): only `ResolveAuthorTomlFileAsync(FileInfo configFile, CancellationToken)` and private `TryResolveLocalAuthorsFile()`.
- **Hypothesis**: an in-progress method was either renamed/removed or never written. A second commented-out copy of the same call exists at `InitCommand.cs:44` (currently inside a `/* ... */` block so it doesn't error).
- **Suggested fix**: write `FileHelper.ResolveExistingConfigFile(IConfiguration configuration)` that resolves to a `FileInfo` pointing at the user-level `AppConfig` (the one that `ConfigSettingsTomlContext` deserializes). Look at `AppConfigRegistryPathResolver` (`src/CoAttribution.Cli/Helpers/AppConfigRegistryPathResolver.cs:30`) for the existing `IConfiguration["config-file"]` / `IConfiguration["coauthor_config_file"]` lookup pattern. Return type should likely be `FileInfo` (the consumer immediately calls `.FullName`).

### E2 — `AddCoAuthorCommand._configuration` does not exist

- **File**: `src/CoAttribution.Cli/Commands/AddCoAuthorCommand.cs:52`
- **Error**: `error CS0103: The name '_configuration' does not exist in the current context`
- **Call site**:
  ```csharp
  AppConfig configuration = await _configResolver.ResolveAppConfig(_configuration, cliContext.CancellationToken);
  ```
- **What the class actually has**: only `_authorRegistry` and `_configResolver` are injected (see constructor at `AddCoAuthorCommand.cs:24-29`). The commented-out TUI dialog block above (lines 10-12 and 54-78) suggests the original design accepted the config through a different path.
- **Hypothesis**: a refactor split the file from a self-sufficient command (which had its own `IConfiguration` field, like `ConfigCommand` and `InitCommand`) into a delegating command (which now relies on `IConfigResolver`). The `_configuration` reference was missed during that refactor.
- **Suggested fix**: either (a) add `IConfiguration configuration` to the constructor and store it as `_configuration`, or (b) remove the `_configuration` argument from the `_configResolver.ResolveAppConfig` call and let the resolver pull it from its own injected `IConfiguration`. Option (a) matches the rest of the codebase; option (b) requires changing `IConfigResolver.ResolveAppConfig`'s signature.

### E3, E4 — `AddCoAuthorCommand.authorsFile` does not exist (×2)

- **File**: `src/CoAttribution.Cli/Commands/AddCoAuthorCommand.cs:99` and `:105`
- **Error**: `error CS0103: The name 'authorsFile' does not exist in the current context` (twice)
- **Call sites**:
  ```csharp
  Console.Out.WriteLine(Resources.Commands_Authors_Add_Successful, newCoAuthor, authorsFile.FullName);  // line 99
  Console.WriteLine(Resources.Commands_Authors_Add_Failed, newCoAuthor, authorsFile.FullName);          // line 105
  ```
- **Hypothesis**: `authorsFile` was a local `FileInfo` populated earlier in `RunAsync` (likely from `FileHelper.ResolveAuthorTomlFileAsync(...)` or from a resolved `AppConfig.PathsSettings["global_registry"]` lookup, the same pattern at `FileHelper.cs:33-38`). It is referenced in both the success and failure branches but never declared.
- **Suggested fix**: populate `authorsFile` from the resolved `AppConfig` before the try block:
  ```csharp
  FileInfo? authorsFile = new(configuration.PathsSettings["global_registry"]);
  ```
  (or use `FileHelper.ResolveAuthorTomlFileAsync(...)` if a `FileInfo configFile` is available). Make sure both branches can see it; the success branch is only entered when `_authorRegistry.AddAsync` returns, but the failure branch is entered when an exception is thrown — both need the same scope.

### E5 — `InitCommand.Config` does not exist

- **File**: `src/CoAttribution.Cli/Commands/InitCommand.cs:63`
- **Error**: `error CS0103: The name 'Config' does not exist in the current context`
- **Call site**:
  ```csharp
  if(Config)

  await CreateConfigFileAsync(cliContext.CancellationToken);
  ```
- **Hypothesis**: a half-typed condition (probably meant `if (ConfigFilePath)` or `if (string.IsNullOrEmpty(ConfigFilePath))`). The line is a dangling `if` with an empty body and a missing semicolon, which is also a syntax smell.
- **Suggested fix**: this looks like an incomplete TUI integration. The commented-out TUI block above (lines 28-59) suggests the original design had an `Interactive` flag plus this conditional. The least-invasive fix is to remove the `if(Config)` line entirely (it is currently a no-op anyway and the call to `CreateConfigFileAsync` runs unconditionally). A more complete fix would be to wire the conditional to a real boolean option.

### E6 — `RemoveCoAuthorCommand.authorsFile` does not exist

- **File**: `src/CoAttribution.Cli/Commands/RemoveCoAuthorCommand.cs:40`
- **Error**: `error CS0103: The name 'authorsFile' does not exist in the current context`
- **Call site**:
  ```csharp
  Console.WriteLine(Resources.Commands_Authors_Remove_Failed, string.Join(", ", Ids), authorsFile.FullName);
  ```
- **Hypothesis**: same pattern as E3/E4 — the success branch of `RunAsync` (line 33-36) doesn't need `authorsFile`, but the failure branch does for an error message, and the variable was never declared.
- **Suggested fix**: inject `IConfigResolver` and resolve `authorsFile` from the resulting `AppConfig.PathsSettings["global_registry"]` before the try block, mirroring the E3/E4 fix. Alternatively, this is a logging-only use — the command could be simplified to log just the IDs and exception details without the file path.

## Patterns to look for

All six errors share a common root cause: **a partial refactor of the `*CoAuthorCommand` files** that left orphaned references to symbols that were either (a) moved to a different file, (b) removed in favor of an injected resolver, or (c) never written. The other `*CoAuthorCommand` files (`ListCoAuthorsCommand`) and the `*CoAuthor*` infrastructure in `CoAttribution.Lib` compile cleanly and serve as a working reference.

A future agent tackling this ticket should:

1. **Read `ListCoAuthorsCommand.cs` end-to-end** as a working template — it correctly resolves `AppConfig` via `_authorRegistry.GetAuthorConfigAsync` and uses the existing `GetCoAuthors()` extension, without needing `FileHelper.ResolveExistingConfigFile` or a local `authorsFile`.
2. **Resolve E2 and E3/E4 together** — they all live in `AddCoAuthorCommand` and the fix likely involves adding a single `_configResolver` call at the top of `RunAsync` that produces both `configuration` and `authorsFile`.
3. **Treat E5 as a typo, not a feature** — the `if(Config)` line is almost certainly a half-written condition. Confirm with the project owner before adding a new option; the simplest correct fix is to delete the line.
4. **Audit the commented-out TUI blocks** in `AddCoAuthorCommand.cs` (lines 10-12, 54-78) and `InitCommand.cs` (lines 12-14, 28-59) — they reference types like `AddAuthorDialog`, `SetupDialog`, `IApplication`, and `MessageBox` that may also be missing or in a half-implemented state.

## What is NOT in this ticket

- The `ConfigSettingsTomlContext` layering fix (already shipped).
- The `CommitOrchestrator.cs:37` regression fix (already shipped).
- Host-override DTOs / validator / default map / `HostBlockWriter` (already shipped under separate tickets).
- Host-override abstractions, implementations, and presentation: `IHostResolver`, `IGitConfigClient`, `IGitRemoteProbe`, `HostResolver`, `GitConfigClient`, `GitRemoteProbe`, `MissingHostBlockDialog`, `MissingHostBlockChoice`, `MissingHostBlockDiagnosticFormatter`, and the 5 new resource strings in `Resources.resx` / `Resources.Designer.cs` (already shipped under Tickets 3, 4, and 6).
- Any change to the public API surface of `IAuthorRegistry` or `IConfigResolver`. (`AppConfig` had its namespace moved from `CoAttribution.Cli.Models` to `CoAttribution.Lib.Models`; the type's members are unchanged.)

## Reproduction

```bash
cd C:\Users\alast\scoop\buckets\copilot-worktrees\CoAttribution\alastairlundy-symmetrical-guide
dotnet build src/CoAttribution.slnx
```

Expected output (as of 2026-07-17): `Build FAILED. 6 Error(s) 0 Warning(s)`.
