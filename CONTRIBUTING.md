# Contributing to CoAttribution

Thank you for your interest in contributing to CoAttribution. This guide explains how to contribute effectively to this project.

## Before You Start

CoAttribution is strictly an **Attribution Metadata Orchestrator** — it manages the 'Who' and 'How' of Git commit trailers. Review `README.md` and `GLOSSARY.md` to understand the project's scope and boundaries before contributing.

Contributions that fall outside the project's scope or roadmap may not be merged, even if well-intentioned. If you're unsure whether a change is in scope, open an issue to discuss it first.

## How to Contribute

### 1. Clone the Repository

```bash
git clone https://github.com/alastairlundy/CoAttribution.git
cd CoAttribution
```

### 2. Create a Branch

Create a separate branch for each issue or feature you want to work on:

```bash
git checkout -b your-branch-name
```

Use a short, descriptive branch name (e.g. `fix-tui-crash`, `add-dry-run-output`).

### 3. Implement and Test

Make your changes and verify they work:

```bash
dotnet build src/CoAttribution.slnx
dotnet test src/CoAttribution.slnx
```

Ensure NativeAOT compatibility is maintained — the CLI must build with `IsTrimmable` and `IsAotCompatible` enabled. See `docs/adr/0001-native-aot-constraint.md`.

## Scope Boundaries

CoAttribution has hard design boundaries that must not be crossed:

- **No Index Manipulation**: Do not call `git add` or manage the staging area. The tool assumes changes are already staged.
- **No Intelligence**: Do not integrate with LLMs or generate commit messages. The tool only appends trailers to user-provided messages.
- **No Remote Integration**: Do not contact GitHub/GitLab/Bitbucket APIs. Relies entirely on local config files and local Git binaries.
- **No Prompt Management**: Do not add prompt engineering or AI agent management features.

### 4. Submit a Pull Request

Once your changes are implemented and tested, push your branch and open a Pull Request:

```bash
git push origin your-branch-name
```

## Pull Request Guidelines

### One PR, One Issue

Each PR should address a single issue or feature. Focused PRs are easier to review and less likely to introduce conflicts. If you want to address multiple issues, open separate PRs for each.

### Drive-By PRs

Drive-by PRs are welcome. However, contributions that are out of scope of the repository and its roadmap may not get merged. Check the project scope in `README.md` before investing time in a change.

### AI Usage Declaration

If any part of your contribution was generated or assisted by AI, you **must** declare this in your PR description. Include:

- **What** was AI-generated or AI-assisted (e.g. code, tests, documentation, commit messages)
- **Which AI model(s)** were used (e.g. GPT-5.5, Claude Opus 4.8, MiniMax M3)
- **What each model was used for** if multiple models were involved
- **What AI agent harness** was used (e.g. GitHub Copilot, Cursor, Claude Code, OpenCode)

Example:

> **AI Usage**: Code in `src/CoAttribution.Lib/TomlParser.cs` was AI-assisted using GitHub Copilot (GPT-5.5) via the Copilot CLI agent harness. Tests were written manually.

### PR Descriptions

PRs authored by external contributors must not have AI-generated PR descriptions. You may use AI to help guide your writing or suggest wording, but the final PR description must be written by a human.

PRs that do not follow this guide may not be merged.

## Code Conventions

- Follow the existing project structure: CLI code in `src/CoAttribution.Cli/`, library code in `src/CoAttribution.Lib/`.
- Reusable logic belongs in the library project, not the CLI.
- Maintain NativeAOT compatibility — avoid using reflection and check analyzer warnings.
- Follow the existing code style in the repository.

## Questions?

Open a GitHub issue if you have questions about contributing or the project's direction.
