# CoAttribution

CoAttribution is a CLI that streamlines Git commit attribution by appending `Co-authored-by` and `Assisted-by` trailers using a preset author registry.

## Key Features
- **Author Presets**: Alias-to-identity mapping via TOML.
- **TUI**: Ergonomic, interactive author selection.
- **CLI Wrapper**: Appends attribution and executes `git commit`.
- **Dry-run**: Preview messages without execution.
- **NativeAOT**: Optimized for instant startup.

## Benefits
- **Speed**: Eliminates manual entry of co-author details.
- **Automation**: Simplifies attribution for AI agents.
- **Consistency**: Standardises Git trailer formatting.

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
