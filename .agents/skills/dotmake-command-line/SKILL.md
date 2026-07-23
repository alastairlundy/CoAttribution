# dotmake-command-line

---
name: dotmake-command-line
description: >-
  Guides agents through DotMake.CommandLine conventions used in this repo —
  entry-point setup, standalone Parent-based command hierarchy, the
  System.CommandLine naming-collision gotcha, and alias management. Prevents
  runtime failures from incorrectly structured command classes or conflicting
  short-form aliases.
license: MIT
---

## When to Use

- When you need to add, modify, or review a CLI command class.
- When working with `Program.cs` or the `Cli.RunAsync<T>` entry point.
- When the user asks about `RootCommand`, `System.CommandLine`, or `[CliCommand]`/`[CliOption]`/`[CliArgument]` attributes.
- When troubleshooting a runtime error from `DotMake.CommandLine` (e.g. "should have [CliCommand] attribute" or "CommandAlias conflicts with").
- When the task is ambiguous about which subcommand pattern to use, load the `ask-questions` skill to clarify.

## When Not to Use

- When the task does not involve the CLI framework layer (e.g. pure library changes, business logic, or data access).
- When the user explicitly asks to use a different CLI parsing library.
- When the change is in the TUI layer (`Terminal.Gui` windows and dialogs) rather than command-line parsing.

## Workflow

### Step 1: Identify the change scope
Determine whether the task involves adding/modifying a command class, the entry point, or troubleshooting a runtime error.

**Completion signal:** A specific scope is selected — command class, entry point, or troubleshooting.

### Step 2: Verify the command location and pattern
All command classes must live in `src/CoAttribution.Cli/Commands/` as standalone (non-nested) classes. If the class is a subcommand, its `[CliCommand]` attribute must include `Parent = typeof(ParentCommandClass)`.

**Completion signal:** The command class path and attribute pattern are confirmed correct or corrected.

### Step 3: Check for the System.CommandLine naming collision
In `Program.cs`, confirm there is NO `using System.CommandLine;` directive. If present, remove it — it shadows the custom `RootCommand` class at compile time.

**Completion signal:** `Program.cs` is free of `using System.CommandLine;` (either confirmed absent or removed).

### Step 4: Resolve alias conflicts
If a runtime error reports a short-form alias conflict, either:
- Set `ShortFormAutoGenerate = CliNameAutoGenerate.None` on the root command's `[CliCommand]` to disable all auto-generated aliases (inherited by subcommands), or
- Add an explicit `Alias = "unique-value"` to the conflicting command's `[CliCommand]` attribute.

**Completion signal:** All sibling commands have unique short-form aliases (either by explicit `Alias` or by disabling auto-generation).

### Step 5: Verify the entry point signature
Confirm `Program.cs` calls `Cli.RunAsync<RootCommand>(args, settings)` and that DI is wired via `Cli.Ext.ConfigureServices(...)` before the run call. The generic type must resolve to `CoAttribution.Cli.Commands.RootCommand`.

**Completion signal:** The entry point structure is verified and matches the expected pattern.

## Validation

- [ ] **Command location**: Every command class is in `src/CoAttribution.Cli/Commands/` — not nested inside another class.
- [ ] **Subcommand pattern**: Every subcommand's `[CliCommand]` uses `Parent = typeof(...)` — no nested-class hierarchy.
- [ ] **No System.CommandLine collision**: `Program.cs` does not have `using System.CommandLine;`.
- [ ] **Alias uniqueness**: No two sibling commands have the same auto-generated or explicit short-form alias.
- [ ] **Entry point shape**: `Cli.RunAsync<RootCommand>(args, settings)` is the sole top-level call after `Cli.Ext.ConfigureServices(...)`.
- [ ] **Root command attribute**: `RootCommand` has `[CliCommand]` with no `Name` property (bare or with `ShortFormAutoGenerate` only).
