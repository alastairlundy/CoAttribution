# CoAttribution

CoAttribution is a command-line tool that tags your Git commits with `Co-authored-by` and `Assisted-by` trailers, pulling author identities from a local TOML registry.

## Key Features
- **Author registry**: Map short aliases to names and emails in a TOML file.
- **TUI**: Pick authors interactively from a terminal UI.
- **Commit wrapping**: Adds the trailers, then runs `git commit` for you.
- **Dry-run**: Print the resulting message without committing.
- **NativeAOT**: Compiled ahead-of-time for near-instant startup.

## Why use it
- **Less typing**: No more hand-writing co-author blocks on every commit.
- **Agent-friendly**: Let AI coding agents attribute themselves without ceremony.
- **Consistent**: One format, applied the same way every time.

## Installation

### Via NuGet (as a .NET tool)

Install CoAttribution globally as a dotnet tool:

```bash
dotnet tool install --global CoAttribution
```

To update to the latest version:

```bash
dotnet tool update --global CoAttribution
```

See the package page for current versions: [nuget.org/packages/CoAttribution](https://www.nuget.org/packages/CoAttribution/).

### Via GitHub Releases

Download the latest prebuilt binary for your platform from the [GitHub Releases](https://github.com/alastairlundy/CoAttribution/releases) page, then extract it and place the `co-attr` executable on your `PATH`.

## Usage

### Initialise configuration

```bash
co-attr init
```

Creates a global config file and an `authors.toml` registry pre-populated with common AI agents.

### Add a co-author

```bash
co-attr author add my-agent --type agent --name "Agent Name" --email "agent@example.com"
```

### List registered authors

```bash
co-attr author list
```

### Commit with co-author attribution

```bash
git add .
co-attr commit -m "Implement new feature" --coauthor copilot
```

This appends a `Co-authored-by: copilot <copilot@github.com>` trailer to the commit message and runs `git commit`.

### Commit with assisted-by attribution

```bash
git add .
co-attr commit -m "Fix bug" --assist kilo
```

### Commit with multiple authors

```bash
co-attr commit -m "Refactor module" --coauthor copilot --assist kilo
```

### Dry-run (preview without committing)

```bash
co-attr commit -m "Draft change" --coauthor copilot --verbose
```

## Project Scope

CoAttribution is an Attribution Metadata Orchestrator. Its purpose is to manage the 'Who' and 'How' of Git commit trailers.

### In Scope
- Maintaining a local TOML-based registry of authors (name and email)
- Providing an interactive TUI/CLI for selecting authors and composing commit messages
- Appending `Co-authored-by` and `Assisted-by` trailers to commit messages
- Wrapping the `git commit` command

### Out of Scope
- **Staging changes**: The tool does not run `git add` or manage the staging area. Changes must already be staged before invoking the tool.
- **Message generation**: The tool does not integrate with LLMs or generate commit messages. It only appends trailers to messages provided by the caller.
- **Remote services**: The tool does not communicate with GitHub, GitLab, Bitbucket, or any other remote API. It operates entirely on local configuration and local Git.
- **Prompt or agent management**: The tool is not designed for prompt engineering or AI agent orchestration.

## License
This project is licensed under the MPL 2.0.
