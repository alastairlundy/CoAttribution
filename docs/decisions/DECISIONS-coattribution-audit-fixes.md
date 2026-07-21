# Decision Ledger: CoAttribution Audit Fixes

**Session:** 2026-07-21
**Spec:** `CoAttribution-handoff-2026-07-21-full-audit.md`

### [D001] — session goal

- **Driver**: the handoff document identified 12 issues (4 critical, 3 high, 2 medium, 3 low) that will cause runtime failures or incorrect behaviour; the user wants a usable CLI.
- **Resolved Answer**: "Implementing the fixes that make sense and getting the Cli to a point where it's usable."
- **Normalized Requirement**: The CLI shall boot, accept the 12 commands without crashing on instantiation, resolve config paths correctly, show and set config values without null-reference errors, and produce a valid commit message with co-author trailers.
- **Constraints**: NativeAOT compatibility must be maintained. The tool's scope is strictly "Attribution Metadata Orchestrator" per AGENTS.md — no index manipulation, no LLM integration, no remote APIs.

### [T001] — DI Registration scope

- **Driver**: making the CLI usable with minimal risk requires the least invasive change; the user explicitly noted this can be re-examined later.
- **Resolved Answer**: Option 1 — inline `services.AddSingleton<ICommitOrchestrator, CommitOrchestrator>();` in Program.cs.
- **Normalized Requirement**: Program.cs shall register `ICommitOrchestrator` → `CommitOrchestrator` as a singleton in the existing `ConfigureServices` block.
- **Constraints**: May be refactored into an extension method in a future session. Must not introduce a new project dependency or DI convention.
- **Cites**: D001.

### [T002] — Config path key unification

- **Driver**: the CLI must work with either the default computed config path or a user-specified `--config-path` override, and the configuration-path handling must be resilient — no silent nulls, no invisible Properties storage.
- **Resolved Answer**: Option 1 — `AddCommandLine` switch mapping for `--config-path` → `"config-file"`, plus `AddInMemoryCollection` for the default path under the same key when no `--config-path` is provided. All consumers standardize on the `"config-file"` key.
- **Normalized Requirement**: Program.cs shall (a) map `--config-path` to the IConfiguration key `"config-file"` via `AddCommandLine` switch mappings, (b) inject the computed default path via `AddInMemoryCollection` under `"config-file"` when `--config-path` is absent, and (c) remove all fallback chains from ConfigCommand, ConfigResolver, and AppConfigRegistryPathResolver so they query only `"config-file"`.
- **Constraints**: The `"coauthor_config_file"` and `"config-path"` fallback keys are removed from all consumers. The `-?` replacement in switch mappings must use a dash in the mapping key (not a colon). If `AddCommandLine` switch mappings conflict with how existing args are parsed, the agent shall add `--config-path` to the switch mapping and remove any mapping collision before the next build.
- **Cites**: D001.

### [T003] — ConfigCommand AllowedValues consistency

- **Driver**: the global registry path is an authors-registry property, not a generic paths property; every AllowedValue must be resolvable by the prefix-matching logic.
- **Resolved Answer**: Option 1 — move `authors.global.path` from `PathsSettings` to `AuthorsRegistry` (as `paths.global`), and add `"authors_registry.paths.global"` to AllowedValues.
- **Normalized Requirement**: ConfigCommand's AllowedValues shall use only prefixed keys (`path.*`, `trailers.*`, `tui.*`, `authors_registry.*`). The `authors.global.path` entry is removed from AllowedValues and from the `PathsSettings` dictionary in the AppConfig model; it is added to `AuthorsRegistry` as `paths.global`. The `authors_registry.` prefix branch resolves it.
- **Constraints**: No new prefix branches added. Zero migration concern — no released users.
- **Cites**: D001.

### [T004] — host_aliases config coverage

- **Driver**: the `host_aliases` section is populated at config-deserialization time and consumed internally by `HostResolver`; there is no user-facing need for `config get/set` access.
- **Resolved Answer**: Option 3 — skip the fix; `host_aliases` is internal-only.
- **Normalized Requirement**: No changes to ConfigCommand prefix matching or AllowedValues for `host_aliases`. The `HostAliases` dictionary continues to be populated from the TOML file and consumed by `HostResolver` at runtime.
- **Constraints**: If a future feature requires CLI access to host aliases, this decision should be revisited and the AllowedValues constraint design reconsidered.
- **Cites**: D001.

### [T005] — Async deadlock fix strategy

- **Driver**: follow .NET async best practices ("async all the way") and prepare the async signatures so that the future `IHostResolver` wiring branch has no interface refactoring to do.
- **Resolved Answer**: Option 1 — full async refactor. `IGitConfigClient.TryGet` becomes `Task<bool> TryGetAsync(string key)`, `Set` becomes `Task SetAsync(string key, string value)`, and `IHostResolver.ResolveHost` becomes `Task<HostResolutionResult> ResolveHostAsync(string? hostInput)`.
- **Normalized Requirement**: `IGitConfigClient` shall expose `Task<bool> TryGetAsync(string key)` and `Task SetAsync(string key, string value)`. `GitConfigClient` shall implement both with `await _processInvoker.ExecuteBufferedAsync(...)` and no blocking calls. `IHostResolver` shall expose `Task<HostResolutionResult> ResolveHostAsync(string? hostInput)`. `HostResolver` shall replace all `.GetAwaiter().GetResult()` calls with `await`. The `HostResolver` itself is not wired into DI in this session.
- **Constraints**: The `[NotNullWhen(true)]` pattern on `TryGet` is replaced with a `(bool found, string? value)` tuple return or a `GitConfigResult` type on the async variant. The async suffix convention is followed for all new and renamed async methods.
- **Cites**: D001.

### [T006] — Stub implementation content

- **Driver**: `CreateAuthorsTomlFileAsync` must produce a predictable authors file without surprising the user; the content should ship inside the binary so it's never missing at runtime.
- **Resolved Answer**: Option 1 — embed `DEFAULT_AUTHORS.toml` as a manifest resource, inject `IRegistryPathResolver` into `InitCommand`, write the embedded content to the resolved global registry path (when `CreateGlobalFile` is true) or to the local `.coauthor/authors.toml` (when false).
- **Normalized Requirement**: `DEFAULT_AUTHORS.toml` shall be added to the `.csproj` as `<EmbeddedResource>`. `InitCommand` shall accept `IRegistryPathResolver` via constructor injection. `CreateAuthorsTomlFileAsync` shall read the embedded resource via `Assembly.GetExecutingAssembly().GetManifestResourceStream()`, and write the content to the path resolved by `IRegistryPathResolver` (global) or the current working directory (local).
- **Constraints**: Must be NativeAOT-compatible. Embedded resource access uses exact case-sensitive name matching. `InitCommand` does not need `IRegistryPathResolver` registered separately — it is already registered in `Program.cs`.
- **Cites**: D001.

### [Issues 7, 8, 11, 12] — Straightforward direct edits

- **Issue 7** — `ListCoAuthorsCommand.cs` catch block: change rethrow to `return 1`.
- **Issue 8** — `Resources.resx` `Commands.Authors.Remove.Failed`: change format string to accept 2 args (file path and error message), or change the calling code to pass 1 arg.
- **Issue 11** — Remove commented-out `using Terminal.Gui.*` blocks from `AddCoAuthorCommand.cs:10-12`, `InitCommand.cs:12-14`, `RootCommand.cs:10-13`.
- **Issue 12** — Remove empty `<Folder Include="Components\Windows\" />` from `CoAttribution.Cli.csproj:62`.

### [Issue 10] — TUI files retained

- The 6 TUI component files (`Components/Dialogs/*`, `Components/Windows/*`) are retained for a future TUI development session. Their associated `/* ... */` commented-out code in `RootCommand.cs` and `InitCommand.cs` is also retained.
- No changes to the TUI files or their corresponding csproj Folder directives.

### [T007] — IHostResolver consumer

- **Driver**: the resolved host key should auto-populate default co-author IDs so users don't need to pass `--with` when the host is resolvable, and both `commit` and `message` commands should benefit without interface changes.
- **Resolved Answer**: Option A — `IHostResolver` is injected into `CommitOrchestrator`; `BuildCommitMessageAsync` resolves the host and merges the result into `defaultIds` before calling `AttributionPolicy.Resolve`. Option B (separate `HostAwareCoAuthorResolver` service) is acknowledged for future exploration if the separation proves necessary.
- **Normalized Requirement**: `CommitOrchestrator` shall accept `IHostResolver` via constructor injection. `BuildCommitMessageAsync` shall call `ResolveHostAsync` and append the resolved host key to the default IDs set before resolving co-authors. The `commit` and `message` commands require no changes.
- **Constraints**: Wiring of `IHostResolver` is deferred until after T005 (async refactor) and T002 (config-path fix enabling `AppConfig` resolution). May be refactored into a dedicated resolver service in a future session.
- **Cites**: D001, T001, T005, T006.

<!-- next-id: D002 -->
<!-- next-id: T008 -->
