# Decision Ledger: Author Resolution

### [D001] — priority order for duplicate IDs

- **Resolved Answer**: "I'm not married to the idea of it being a core part of attribution policy but it makes for a reasonably sensible default."
- **Normalized Requirement**: The attribution resolution shall deduplicate author IDs using a priority order of `CoAuthorIds` -> `AssistIds` -> `DefaultIds` as a default behavior.
- **Constraints**: None.

### [D002] — location of the attribution policy

- **Resolved Answer**: "For now (1) but if it expands in size we need to consider moving to (2)"
- **Normalized Requirement**: The deduplication, mapping, and joining logic shall move into `CommitOrchestrator`, and the `CoAuthorResolver` module shall be deleted.
- **Constraints**: If the `CommitOrchestrator` expands in size and its core responsibility becomes diluted, the attribution policy shall be extracted into a dedicated `AttributionPolicy` module.

### [D003] — fate of the message builder

- **Resolved Answer**: "I'm going to go with a hybrid. We keep the builder interface and class from (1) but we reduce the number of calls needed. Allowing for a deeper builder with an interchangeable implementation."
- **Normalized Requirement**: The `ICommitMessageBuilder` and `CommitMessageBuilder` modules shall be retained, but the interface methods shall be reduced to minimize the number of calls required by the orchestrator, supporting a deeper builder with an interchangeable implementation.
- **Constraints**: The specific set of retained and removed methods is subject to a separate decision.

### [D004] — shape of the builder interface

- **Resolved Answer**: "(1) but we keep the Clear method. This may be helpful in the future - it's not currently used by the orchestrator and the orchestrator pays no penalty/tax due to that method's existence. So the methods we end up with are: SetContent, AddCoAuthors, Build, Clear. And the orchestrator only makes: SetContent, AddCoAuthors, Build"
- **Normalized Requirement**: The `ICommitMessageBuilder` interface shall be reduced to four methods: `SetContent(string subject, string body)`, `AddCoAuthors(IEnumerable<ResolvedCoAuthor>)`, `Build()`, and `Clear()`. The orchestrator shall only call `SetContent`, `AddCoAuthors`, and `Build`.
- **Constraints**: The `Clear` method shall be retained for future use, despite not being called by the current orchestrator.

### [D005] — separation of policy logic from I/O

- **Resolved Answer**: "(2)"
- **Normalized Requirement**: The deduplication, mapping, and joining logic shall be extracted into a pure static `AttributionPolicy.Resolve` method. The `CommitOrchestrator` shall call this method after retrieving authors from the registry.
- **Constraints**: None.
