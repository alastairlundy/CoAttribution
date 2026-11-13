# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

Targeted for release 0.2.0. The GitHub release for 0.2.0 is currently a draft.

### Added
- Interactive Terminal UI (Terminal.Gui v2) launched when running `co-attr` with no subcommand on a TTY; non-TTY sessions print help instead. Includes a repository context header, author selection panel, subject/body commit form with a live N/72 counter, commit preview with confirmation, a first-run setup dialog, in-TUI author creation, a quit dialog with draft-on-quit, a missing-host recovery dialog, a status bar, feedback toasts, and file-based logging with theme configuration.
- TrueColour schemes with a 16-colour fallback based on terminal capability, plus a reflection-free GlyphSet.
- Body character counter with hard caps on subject and body.
- `--registry` option (recursive) to override the author registry path on any command.
- `UnknownCoAuthorException`: unknown co-author ids now fail loudly instead of being silently dropped.
- ReleaseGen manifest and release workflow (trusted publishing, per-OS build matrix, RID subsets, tag checkout in the publish job) replacing dotnet-releaser.
- README badges.

### Changed
- The TUI is now always compiled in; the `#if TUI` / `EnableTui` feature flag was removed.
- Migrated process invocation to CliInvoke v3.
- Upgraded Terminal.Gui to 2.5.x with the editor split out to `Terminal.Gui.Editor`; migrated the deprecated TextView and the static `Application` API.
- Performance: registry file lookup cached per working directory, repository context made async and memoised (no sync-over-async on the UI thread), and allocation trims in the commit flow (PR #23).
- `init` now always overwrites `config.toml` and writes the `global_registry` path.
- Default `authors.toml`: removed the `generic_llm` placeholder entries and added the cursor base identity.
- Dependabot schedules tuned with a version cooldown.
- Removed the unused `Microsoft.Extensions.Configuration.CommandLine` package.
- Issue templates and agent documentation updated.

### Fixed
- `AppConfig` is now registered in the DI container: `commit`, `message`, and the TUI no longer fail with `InvalidOperationException` when `HostResolver` is resolved. Service registrations moved out of `Program.cs` into `AppServiceCollection.AddCoAttributionServices` so tests can build the real container.
- `--trailer` output: trailers are now emitted one per line instead of a single collapsed value, which git previously rejected with exit 129.
- Attribution silent failures and registry preservation in `AuthorRegistry`.
- `--config-path` is now accepted by DotMake.CommandLine.
- Multiple TUI rendering and navigation fixes: dialog sizing and key bindings, subject field focus, body editor scrollbars, status-bar shortcut handling, subject/body styling, `Dim.Fill` layout, and Ctrl+Enter behaviour.
- `author remove` prints a success confirmation on stdout.
- Terminal.Gui.Editor upgraded from 2.2.6 to 2.5.7 for Terminal.Gui 2.4.17+ compatibility.
- Release readiness review findings: README dry-run documentation, cursor author identity, stale AGENTS.md claim about the TUI feature flag, and a no-op CI build step.

### Removed
- dotnet-releaser configuration, replaced by ReleaseGen.
- v1 TUI scaffolding files.

## [0.1.1.1] - 2026-09-07

### Added
- Packaged as a .NET tool with an explicit `PackAsTool` and `ToolCommandName` of `co-attr`. The 0.1.1 package shipped without `DotnetToolSettings.xml` and could not be installed as a tool.

## [0.1.1] - 2026-09-07

### Added
- README usage examples.

### Changed
- Updated NuGet package dependencies.

### Security
- Added explicit `permissions` to the CI test workflow.

## [0.1.0.1] - 2026-08-07

### Fixed
- Republished the initial release as the first GitHub release ("Initial Release") with the 0.1.0.1 version. No source changes versus 0.1.0; both tags point at the same commit.

## [0.1.0] - 2026-08-07

### Added
- Initial release of the `co-attr` CLI tool.
- Commands: `init`, `author add`, `author list`, `author remove`, `commit`, `message` (dry-run), and `config`.
- Local TOML author registry with `Co-authored-by` and `Assisted-by` trailer support on commit.
- Packaged as a .NET global tool (`dotnet tool install --global CoAttribution`).
- NativeAOT support for prebuilt binaries from GitHub Releases.
- TUnit test suite and CI workflow.
- dotnet-releaser release pipeline.
- Per-host identity override in `CommitOrchestrator`.
- Default `authors.toml` registry pre-populated with common AI agents.

### Security
- Fixed command injection in release workflow inputs.