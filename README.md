# CoAttribution

CoAuthorCli streamlines Git commit attribution by appending `Co-authored-by` and `Assisted-by` trailers using a preset author registry.

## Key Features
- **Author Presets**: Alias-to-identity mapping via TOML.
- **TUI**: Ergonomic, interactive author selection.
- **CLI Wrapper**: Appends attribution and executes `git commit`.
- **Dry-run**: Preview messages without execution.
- **NativeAOT**: Optimized for instant startup.

## Benefits
- **Speed**: Eliminates manual entry of co-author details.
- **Automation**: Simplifies attribution for AI agents.
- **Consistency**: Standardizes Git trailer formatting.

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
