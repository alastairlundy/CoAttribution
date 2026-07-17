---
title: Builder interface reduction and AttributionPolicy extraction
classification: Independent
blocked_by: ["004-host-resolution-implementations"]
parent: "Conversation context (2026-07-17) - Implementing per-host identity overrides in the attribution registry and the host-resolution precedence chain that selects which override renders for a given commit. Agreed on 4-step host precedence chain, strongly-typed host blocks, and source-generated validation. Out of scope - coattribution doctor subcommand, --save-host persistence, redundant-override linter."
---

## Goal

Refactor the commit message building pipeline per the DECISIONS ledger - reduce the `ICommitMessageBuilder` interface to four methods, extract attribution policy logic into a pure static `AttributionPolicy.Resolve` method, update `CommitOrchestrator` to use the new pipeline, and delete the now-redundant `CoAuthorResolver`.

## What to build

This ticket implements decisions D002 through D005 from the author-resolution decision ledger:

1. **Reduce `ICommitMessageBuilder`** (D003, D004) - the interface is reduced to four methods:
   - `SetContent(string subject, string body)` - replaces the separate `SetSubject` and `SetBody` calls
   - `AddCoAuthors(IEnumerable<ResolvedCoAuthor> coAuthors)` - replaces the per-author `AddCoAuthorById` loop
   - `Build()` - unchanged
   - `Clear()` - retained for future use (not called by the orchestrator)
   The `AddBodyLine` method is removed.

2. **Update `CommitMessageBuilder`** - the concrete implementation is updated to match the reduced interface. `SetContent` replaces `SetSubject` + `SetBody`. `AddCoAuthors` accepts a collection and adds them all at once.

3. **Create `AttributionPolicy`** (D005) - a new pure static class at `src/CoAttribution.Lib/AttributionPolicy.cs` with a `Resolve` method that takes the available authors and the request IDs (CoAuthorIds, AssistIds, DefaultIds), performs deduplication using the priority order `CoAuthorIds` -> `AssistIds` -> `DefaultIds`, and returns `ResolvedCoAuthor[]`. This extracts the deduplication and mapping logic from `CoAuthorResolver` into a pure function with no dependencies.

4. **Update `CommitOrchestrator`** (D002) - the orchestrator now calls `AttributionPolicy.Resolve` after retrieving authors from the registry, then passes the resolved co-authors to the builder's `AddCoAuthors` method. The orchestrator no longer depends on `ICoAuthorResolver`.

5. **Delete `CoAuthorResolver`** and **`ICoAuthorResolver`** - these are replaced by `AttributionPolicy.Resolve`.

## Size

- **Files** - 5 files to create, edit, or delete
  - Edit: `src/CoAttribution.Lib/Builders/ICommitMessageBuilder.cs`
  - Edit: `src/CoAttribution.Lib/Builders/CommitMessageBuilder.cs`
  - Create: `src/CoAttribution.Lib/AttributionPolicy.cs`
  - Edit: `src/CoAttribution.Lib/CommitOrchestrator.cs`
  - Delete: `src/CoAttribution.Lib/CoAuthorResolver.cs`
  - Delete: `src/CoAttribution.Lib/Abstractions/ICoAuthorResolver.cs`

## Recommended Workflow

### Step 1 — Create AttributionPolicy static class

Where: `src/CoAttribution.Lib/AttributionPolicy.cs`

- Create `public static class AttributionPolicy` in namespace `CoAttribution.Lib`
- Implement `public static ResolvedCoAuthor[] Resolve(GitCoAuthor[] availableAuthors, string[] defaultIds, string[] coAuthorIds, string[] assistIds)`
- The method performs the same deduplication logic currently in `CoAuthorResolver.ResolveCoAuthors`:
  - Build a sequence of (id, AttributionType) pairs from coAuthorIds (CoAuthor), assistIds (Assisted), defaultIds (DefaultOrCoAuthor)
  - Apply `DistinctBy` on the id to enforce the priority order (CoAuthorIds first, then AssistIds, then DefaultIds)
  - Join with available authors on CoAuthorId
  - Return `ResolvedCoAuthor[]`
- Add MPL 2.0 license header

Verify: Unit test confirms deduplication priority (an ID in both CoAuthorIds and DefaultIds resolves as CoAuthor, not DefaultOrCoAuthor)

### Step 2 — Reduce ICommitMessageBuilder interface

Where: `src/CoAttribution.Lib/Builders/ICommitMessageBuilder.cs`

- Replace `SetSubject(string subject)` and `SetBody(string text)` with `SetContent(string subject, string body)`
- Replace `AddCoAuthorById(GitCoAuthor coAuthor, AttributionType attributionType)` with `AddCoAuthors(IEnumerable<ResolvedCoAuthor> coAuthors)`
- Remove `AddBodyLine(string text)`
- Keep `Build()` and `Clear()` unchanged
- The interface now has exactly four methods: `SetContent`, `AddCoAuthors`, `Build`, `Clear`

Verify: Interface has exactly four methods

### Step 3 — Update CommitMessageBuilder implementation

Where: `src/CoAttribution.Lib/Builders/CommitMessageBuilder.cs`

- Replace `SetSubject` and `SetBody` with `SetContent(string subject, string body)` that sets both at once
- Replace `AddCoAuthorById` with `AddCoAuthors(IEnumerable<ResolvedCoAuthor> coAuthors)` that adds the collection to the internal list
- Remove `AddBodyLine`
- Keep `Build()` and `Clear()` unchanged

Verify: Builder still produces correct `CommitMessage` objects with the new method signatures

### Step 4 — Update CommitOrchestrator

Where: `src/CoAttribution.Lib/CommitOrchestrator.cs`

- Remove the `ICoAuthorResolver` dependency from the constructor
- In `BuildCommitMessageAsync`, replace the `_coAuthorResolver.ResolveCoAuthors(...)` call with `AttributionPolicy.Resolve(...)`
- Replace the per-author `_commitMessageBuilder.AddCoAuthorById(...)` loop with a single `_commitMessageBuilder.AddCoAuthors(actualCoAuthors)` call
- Replace `_commitMessageBuilder.SetSubject(...)` + `_commitMessageBuilder.SetBody(...)` with `_commitMessageBuilder.SetContent(...)`
- Update the `ICommitOrchestrator` interface if needed to remove any references to `ICoAuthorResolver`

Verify: Orchestrator compiles without `ICoAuthorResolver` dependency; builds a correct commit message

### Step 5 — Delete CoAuthorResolver and ICoAuthorResolver

Where: `src/CoAttribution.Lib/CoAuthorResolver.cs`, `src/CoAttribution.Lib/Abstractions/ICoAuthorResolver.cs`

- Delete `src/CoAttribution.Lib/CoAuthorResolver.cs`
- Delete `src/CoAttribution.Lib/Abstractions/ICoAuthorResolver.cs`
- Remove any remaining references to these types (e.g., DI registration, using statements)

Verify: Solution builds cleanly with no references to deleted types

### Step 6 — Update DI registration and callers

Where: N/A (search for references)

- Search for any DI registration of `ICoAuthorResolver` / `CoAuthorResolver` and remove it
- Search for any callers of the old `ICommitMessageBuilder` methods (`SetSubject`, `SetBody`, `AddBodyLine`, `AddCoAuthorById`) and update them to use the new methods
- Ensure all tests are updated to use the new interface

Verify: Full solution builds; no references to deleted types or old method signatures remain

## Context pointers

**Files**
- `src/CoAttribution.Lib/Builders/ICommitMessageBuilder.cs` - interface to reduce
- `src/CoAttribution.Lib/Builders/CommitMessageBuilder.cs` - implementation to update
- `src/CoAttribution.Lib/CommitOrchestrator.cs` - orchestrator to refactor
- `src/CoAttribution.Lib/CoAuthorResolver.cs` - to be deleted (logic moves to AttributionPolicy)
- `src/CoAttribution.Lib/Abstractions/ICoAuthorResolver.cs` - to be deleted
- `src/CoAttribution.Lib/Models/DTOs/ResolvedCoAuthor.cs` - consumed by AttributionPolicy and builder

**ADRs** - None

**Domain terms**
- Attribution policy - the rules for deduplicating and resolving author IDs into a final set of co-authors
- Builder interface reduction - consolidating multiple fine-grained builder calls into fewer, deeper calls

**Ledger records**
- `DECISIONS-CoAttribution-author-resolution.md#D002` - Location of attribution policy (moves into CommitOrchestrator, CoAuthorResolver deleted)
- `DECISIONS-CoAttribution-author-resolution.md#D003` - Fate of the message builder (keep interface and class, reduce calls)
- `DECISIONS-CoAttribution-author-resolution.md#D004` - Shape of the builder interface (four methods - SetContent, AddCoAuthors, Build, Clear)
- `DECISIONS-CoAttribution-author-resolution.md#D005` - Separation of policy logic from I/O (pure static AttributionPolicy.Resolve)
- `DECISIONS-CoAttribution-agent-trailer-format.md#T013` - Dependency direction (Lib has no references to Cli)

## Acceptance criteria

- [ ] `ICommitMessageBuilder` has exactly four methods: `SetContent`, `AddCoAuthors`, `Build`, `Clear`
- [ ] `SetContent(string subject, string body)` replaces the separate `SetSubject` and `SetBody` methods
- [ ] `AddCoAuthors(IEnumerable<ResolvedCoAuthor>)` replaces the per-author `AddCoAuthorById` method
- [ ] `AddBodyLine` is removed from the interface
- [ ] `CommitMessageBuilder` implementation matches the reduced interface
- [ ] `AttributionPolicy` exists as a `public static class` with a `Resolve` method
- [ ] `AttributionPolicy.Resolve` performs deduplication with priority order CoAuthorIds > AssistIds > DefaultIds
- [ ] `CommitOrchestrator` calls `AttributionPolicy.Resolve` instead of `ICoAuthorResolver`
- [ ] `CommitOrchestrator` calls `SetContent` and `AddCoAuthors` on the builder
- [ ] `CoAuthorResolver.cs` is deleted
- [ ] `ICoAuthorResolver.cs` is deleted
- [ ] No references to deleted types remain in the solution
- [ ] The solution builds without errors or NativeAOT analyzer warnings

## Dependencies

**Blocked by** - `004-host-resolution-implementations` (host resolution must be in place before the orchestrator is refactored to consume it)
