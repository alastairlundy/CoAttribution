# ADR 001: Native AOT Compatibility

## Status
Accepted

## Context
The CoAttribution CLI is distributed as a standalone native executable. Users should be able to download a single binary and run it without installing a .NET runtime. This requires Native AOT publishing. Native AOT imposes several constraints on the codebase: reflection is heavily restricted, source generators must be used for serialization, and dependencies must be trimming- and AOT-compatible.

## Decision
We will maintain Native AOT compatibility across the CLI and library projects, enforced via project settings, AOT analyzers, and dependency selection.

## Rationale
1. **Distribution:** Users can run the tool without installing the .NET SDK or Runtime.
2. **Performance:** Native AOT produces a self-contained executable with faster startup than JIT.
3. **Predictability:** AOT analyzers catch reflection misuse at build time rather than runtime.

## Consequences
- `IsTrimmable`, `IsAotCompatible`, and `EnableAotAnalyzer` are active in project files.
- All serialization uses source-generated contexts (`Tomlyn`, `System.Text.Json`) rather than runtime reflection.
- Dependencies must be vetted for AOT compatibility before adoption.
- The TUI (`Terminal.Gui`) is excluded from the default AOT build via `#if TUI` conditional compilation.
- Reflection-heavy libraries or patterns cannot be used without explicit AOT workarounds.
