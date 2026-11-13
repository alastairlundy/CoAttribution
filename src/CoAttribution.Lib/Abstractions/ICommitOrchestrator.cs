/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Lib.Abstractions;

public interface ICommitOrchestrator
{
    /// <summary>
    /// Builds a <see cref="CommitMessage"/> from a <see cref="CommitRequest"/>.
    /// When <see cref="CommitRequest.Amend"/> is set, the content comes from
    /// the last commit's message (with the managed trailer strip applied only
    /// when at least one attribution flag is present) instead of the supplied
    /// subject and body. Never executes a commit.
    /// </summary>
    Task<CommitMessage> BuildCommitMessageAsync(CommitRequest commitRequest, CancellationToken cancellationToken);

    Task<GitResult> ExecuteCommitAsync(CommitMessage commitMessage, CancellationToken cancellationToken);

    /// <summary>
    /// Amends the last commit: rebuilds the message from HEAD's message with
    /// the current invocation's trailers and executes
    /// <c>git commit --amend</c>.
    /// </summary>
    Task<GitResult> AmendCommitAsync(CommitRequest commitRequest, CancellationToken cancellationToken);
}
