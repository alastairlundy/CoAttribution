# CoAttribution

[![NuGet Downloads](https://img.shields.io/nuget/dt/CoAttribution?style=flat-square)](https://www.nuget.org/packages/CoAttribution/)
[![Latest Version](https://img.shields.io/nuget/v/CoAttribution?style=flat-square)](https://www.nuget.org/packages/CoAttribution/)
[![License](https://img.shields.io/github/license/alastairlundy/CoAttribution?style=flat-square)](LICENSE.txt)

CoAttribution is a command-line tool that tags your Git commits with `Co-authored-by` and `Assisted-by` trailers, pulling author identities from a local TOML registry.

## Key Features
- **Author registry**: Map short aliases to names and emails in a TOML file.
- **TUI**: Pick authors interactively from a terminal UI.
- **Commit wrapping**: Adds the trailers, then runs `git commit` for you.
- **Amend**: Fix attribution on the last commit with `--amend`, without duplicating trailers.
- **Dry-run**: `co-attr message` prints the resulting message without committing.
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

### Amend the last commit

`--amend` reworks attribution on the most recent commit instead of creating a new one:

```bash
co-attr commit -m "Implement new feature" --amend --coauthor copilot
```

With at least one attribution flag (`--coauthor`, `--assist`, or `--with`), CoAttribution reads the last commit's message, strips every existing `Co-authored-by:` and `Assisted-by:` trailer line from it, and applies the current invocation's trailers. All other trailers and footers, such as `Signed-off-by:`, stay untouched. Repeat amends therefore replace the managed trailers rather than duplicating them.

With no attribution flag, `--amend` preserves the last commit's existing `Co-authored-by:` and `Assisted-by:` lines. The strip runs only when at least one attribution flag is present, so a zero-flag amend never removes existing attribution. For deliberate attribution removal, use raw `git commit --amend`.

The `-m` option remains required on `commit`, but amend rebuilds the message from the last commit, so its value goes unused. If there is no commit to amend (no commits yet, or not a repository), the command exits nonzero with git's own diagnosis.

Allowlisted relay flags (see below) and the named `--no-verify`/`--signoff` options reach `git commit --amend` exactly as they reach a normal commit.

### Relay git flags after `--`

CoAttribution relays flags to `git commit` and performs no staging itself. Staging stays user-owned, and that includes `-a`. Pass `-a` after `--` and git stages every tracked change before the commit.

Nothing after `--` reaches git unless it is on the allowlist:

| Flag | What it does |
| --- | --- |
| `-a` | Stages all tracked changes before committing (user-owned staging; CoAttribution runs no staging itself) |
| `--no-verify` | Skips git's pre-commit and commit-msg hooks |
| `--signoff` | Adds a `Signed-off-by:` line |
| `--allow-empty` | Allows an empty commit |
| `--author` | Sets the commit author, attached (`--author="Name <email>"`) or as the next token |
| `--no-gpg-sign` | Disables GPG signing for this commit |

```bash
git add .
co-attr commit -m "Ship it" --coauthor copilot -- --allow-empty --author="Deploy Bot <deploy@example.com>"
```

The relayed flags reach `git commit` unchanged, after the tool's own message, trailer lines, and named flags. Every other token after `--` is rejected before git runs. The command exits with an error that names the token, covering tool-managed flags (`-m`, `-F`, `--message`, `--trailer`, `--amend`), the bootstrap-owned `--config-path` and `--registry`, unknown flags, and any flag you already gave, whether as a named option or earlier in the relay.

`--no-verify` and `--signoff` are also named options, so they work before `--` too:

```bash
co-attr commit -m "Ship it" --signoff --coauthor copilot
```

To ask for a new allowlist entry, open an issue at https://github.com/alastairlundy/CoAttribution/issues.

### Dry-run (preview without committing)

`commit` always runs `git commit`. To preview without committing, use the `message` command — it accepts the same attribution flags (`--with`, `--coauthor`, `--assist`) but only prints the resulting message:

```bash
co-attr message -m "Draft change" --with copilot --coauthor copilot
```

With `--amend`, it previews the amend instead. `co-attr message --amend` prints the last commit's message rebuilt with the same strip-or-preserve rules as `commit --amend`, without ever invoking `git commit`. In amend mode neither `-m` nor `--with` is required:

```bash
co-attr message --amend --coauthor copilot
```

### Stale `[trailers]` config warning

Versions before 1.0 accepted `trailers.default`, `trailers.assistance`, and `trailers.coauthorship` in the config file, but never read them. `co-attr config` no longer accepts the keys. Trailer token text (`Co-authored-by`, `Assisted-by`) comes only from the tool's built-in localized resources.

If your config file still carries a `[trailers]` section, every command that reads the config prints one line to stderr, once per run:

```
Warning: the [trailers] section in config file '<path to config.toml>' is no longer supported; its values are ignored.
```

The values never affect trailer output, and CoAttribution never rewrites your config file. Delete the `[trailers]` section to silence the warning.

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
