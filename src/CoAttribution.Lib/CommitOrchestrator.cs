/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Lib.Builders;

namespace CoAttribution.Lib;

public class CommitOrchestrator : ICommitOrchestrator
{
    private readonly ICommitMessageBuilder _commitMessageBuilder;
    private readonly IAuthorRegistry _authorRegistry;
    private readonly IGitClient _gitClient;

    public CommitOrchestrator(ICommitMessageBuilder commitMessageBuilder,
        IAuthorRegistry authorRegistry,
        IGitClient gitClient)
    {
        _commitMessageBuilder = commitMessageBuilder;
        _authorRegistry = authorRegistry;
        _gitClient = gitClient;
    }
    
    public async Task<CommitMessage> BuildCommitMessageAsync(CommitRequest commitRequest, CancellationToken cancellationToken)
    {
        _commitMessageBuilder.SetContent(commitRequest.MessageSubject, commitRequest.MessageBody);

        GitCoAuthorConfig authorConfig = await _authorRegistry.GetAuthorConfigAsync(cancellationToken);

        GitCoAuthor[] coAuthors = authorConfig.GetCoAuthors();
        
        ResolvedCoAuthor[] actualCoAuthors = AttributionPolicy.Resolve(
            coAuthors, commitRequest.DefaultIds,
            commitRequest.CoAuthorIds, commitRequest.AssistIds);
        
        _commitMessageBuilder.AddCoAuthors(actualCoAuthors);
        
        return _commitMessageBuilder.Build();
    }

    public async Task<GitResult> ExecuteCommitAsync(CommitMessage commitMessage, CancellationToken cancellationToken)
        => await _gitClient.CommitAsync(commitMessage, cancellationToken);
}