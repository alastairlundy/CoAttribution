---
title: Host-key validation and default host map
classification: Independent
blocked_by: []
parent: "Conversation context (2026-07-17) - Implementing per-host identity overrides in the attribution registry and the host-resolution precedence chain that selects which override renders for a given commit. Agreed on 4-step host precedence chain, strongly-typed host blocks, and source-generated validation. Out of scope - coattribution doctor subcommand, --save-host persistence, redundant-override linter."
---

## Goal

Create the two foundational static utilities for host resolution - a source-generated host-key validator and the built-in hostname-to-host-key map. These are consumed by the resolver and other downstream components.

## What to build

Two new static classes under `src/CoAttribution.Lib/HostResolution/`:

1. `HostKeyValidator` - validates that a host key consists of only lowercase ASCII letters (pattern `^[a-z]+$`). Must use `[GeneratedRegex]` source generation so that an invalid pattern fails the build at compile time. Exposes two methods - `IsValid(string? key)` returning a bool for hot-path checks, and `TryValidate(string? key, out string? error)` returning a human-readable diagnostic for error reporting.

2. `DefaultHostMap` - a static read-only dictionary mapping five canonical hostnames to their host keys (github, gitlab, bitbucket, gitea, codeberg). Must use `StringComparer.OrdinalIgnoreCase` for case-insensitive lookups. The gitea entry uses `gitea.com` (not `gitea.io`).

Both classes live in namespace `CoAttribution.Lib.HostResolution`.

## Size

- **Files** - 2 files to create
  - Create: `src/CoAttribution.Lib/HostResolution/HostKeyValidator.cs`
  - Create: `src/CoAttribution.Lib/HostResolution/DefaultHostMap.cs`

## Recommended Workflow

### Step 1 — Create the HostResolution directory

Where: `src/CoAttribution.Lib/HostResolution/`

- Create the `HostResolution` directory under `src/CoAttribution.Lib/` if it does not already exist

Verify: Directory exists

### Step 2 — Create HostKeyValidator

Where: `src/CoAttribution.Lib/HostResolution/HostKeyValidator.cs`

- Create a `static partial class HostKeyValidator` in namespace `CoAttribution.Lib.HostResolution`
- Define `public const string Pattern = "^[a-z]+$";`
- Add `[GeneratedRegex(Pattern)] private static partial bool IsValidHostKey(string? value);`
- Implement `public static bool IsValid(string? key) => IsValidHostKey(key);`
- Implement `public static bool TryValidate(string? key, out string? error)` that returns true if valid, or sets error with a message naming the offending character(s)

Verify: `HostKeyValidator.IsValid("github")` returns true; `HostKeyValidator.IsValid("GitHub")` returns false; `HostKeyValidator.IsValid(null)` returns false

### Step 3 — Create DefaultHostMap

Where: `src/CoAttribution.Lib/HostResolution/DefaultHostMap.cs`

- Create a `public static class DefaultHostMap` in namespace `CoAttribution.Lib.HostResolution`
- Define `public static readonly IReadOnlyDictionary<string, string> Entries` initialized with a `Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)`
- Add exactly five entries: `github.com` -> `github`, `gitlab.com` -> `gitlab`, `bitbucket.org` -> `bitbucket`, `gitea.com` -> `gitea`, `codeberg.org` -> `codeberg`

Verify: `DefaultHostMap.Entries.Count` equals 5; `DefaultHostMap.Entries["GitHub.com"]` returns `"github"` (case-insensitive)

## Context pointers

**Files**
- `src/CoAttribution.Lib/Models/GitCoAuthor.cs` - reference for existing partial class pattern

**ADRs** - None

**Domain terms**
- Host key - a lowercase alphabetic identifier for a git hosting platform (e.g., `github`, `gitlab`)
- Hostname - the DNS name of a git hosting platform (e.g., `github.com`, `gitlab.com`)

**Ledger records**
- `DECISIONS-CoAttribution-agent-trailer-format.md#T008` - DefaultHostMap representation (5 entries, OrdinalIgnoreCase, gitea.com correction)
- `DECISIONS-CoAttribution-agent-trailer-format.md#T009` - Host-key validation rule (lowercase alpha only, source-generated regex)
- `DECISIONS-CoAttribution-agent-trailer-format.md#T020` - HostKeyValidator uses [GeneratedRegex] source generator

## Acceptance criteria

- [ ] `HostKeyValidator` exists as a `static partial class` with `Pattern` const, `IsValid`, and `TryValidate` methods
- [ ] `HostKeyValidator` uses `[GeneratedRegex]` for the regex pattern
- [ ] `HostKeyValidator.IsValid("github")` returns true
- [ ] `HostKeyValidator.IsValid("GitHub")` returns false (uppercase not allowed)
- [ ] `HostKeyValidator.IsValid(null)` returns false
- [ ] `HostKeyValidator.TryValidate` produces a human-readable error for invalid keys
- [ ] `DefaultHostMap` exists as a `public static class` with exactly 5 entries
- [ ] `DefaultHostMap.Entries` uses `StringComparer.OrdinalIgnoreCase`
- [ ] `DefaultHostMap.Entries["gitea.com"]` returns `"gitea"` (not `gitea.io`)
- [ ] The solution builds without NativeAOT analyzer warnings

## Dependencies

**Blocked by** - None - can start immediately
