# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] — Unreleased

### Added
- Interactive Terminal UI: running `co-attr` with no subcommand on a TTY now launches the TUI commit flow — repository context header, author selection panel, subject/body commit form, and commit preview with confirmation.
- First-run `SetupDialog` shown in the TUI when the registry is empty or the tool is not yet initialised.
- Missing-host recovery dialog, draft-on-quit protection, and feedback toasts in the TUI.
- In-TUI author creation via `AddAuthorDialog`.
- File-based logging and Terminal.Gui theme configuration.
- Glyph set with TrueColor scheme support and 16-color fallback based on terminal capability.
### Changed
- Migrated process invocation to CliInvoke v3.
- Upgraded Terminal.Gui to 2.5.x with the editor split out to `Terminal.Gui.Editor`.

### Fixed
- `--trailer` output: trailers are now emitted one per line instead of a single collapsed value.
- Multiple TUI rendering and navigation fixes: body editor scrollbars, subject field focus, status-bar key handling, and dialog layout.
- Removed placeholder author entries from the default `authors.toml` registry.

## [0.1.1] — 2026-09-07

### Changed
- Updated NuGet package dependencies.

### Security
- Added explicit `permissions` to the CI test workflow.

## [0.1.0] — 2026-08-07

### Added
- Initial release of the `co-attr` CLI tool.
- Commands: `init`, `author add`, `author list`, `commit`, `message` (dry-run), and `config`.
- Local TOML author registry with `Co-authored-by` and `Assisted-by` trailer support on commit.
- Packaged as a .NET global tool (`dotnet tool install --global CoAttribution`).
- NativeAOT support for prebuilt binaries from GitHub Releases.
