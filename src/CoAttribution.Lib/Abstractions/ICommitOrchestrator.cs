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
    Task<CommitMessage> BuildCommitMessageAsync(CommitRequest commitRequest, CancellationToken cancellationToken);
    
    Task<GitResult> ExecuteCommitAsync(CommitMessage commitMessage, CancellationToken cancellationToken);
}