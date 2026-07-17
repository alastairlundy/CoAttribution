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
    private readonly ICoAuthorResolver _coAuthorResolver;
    private readonly IGitClient _gitClient;

    public CommitOrchestrator(ICommitMessageBuilder commitMessageBuilder,
        IAuthorRegistry authorRegistry,
        ICoAuthorResolver coAuthorResolver,
        IGitClient gitClient)
    {
        _commitMessageBuilder = commitMessageBuilder;
        _authorRegistry = authorRegistry;
        _coAuthorResolver = coAuthorResolver;
        _gitClient = gitClient;
    }
    
    public async Task<CommitMessage> BuildCommitMessageAsync(CommitRequest commitRequest, CancellationToken cancellationToken)
    {
        _commitMessageBuilder.SetSubject(commitRequest.MessageSubject);
        _commitMessageBuilder.SetBody(commitRequest.MessageBody);

        GitCoAuthorConfig authorConfig = await _authorRegistry.GetAuthorConfigAsync(cancellationToken);

        GitCoAuthor[] coAuthors = authorConfig.GetCoAuthors();
        
        ResolvedCoAuthor[] actualCoAuthors = _coAuthorResolver.ResolveCoAuthors(
            new CoAuthorResolutionRequest(coAuthors, commitRequest.DefaultIds,
                commitRequest.CoAuthorIds, commitRequest.AssistIds));
        
        foreach (ResolvedCoAuthor coAuthorPair in actualCoAuthors)
        {
            _commitMessageBuilder.AddCoAuthorById(coAuthorPair.Author,
                coAuthorPair.Type == AttributionType.DefaultOrCoAuthor
                    ? AttributionType.CoAuthor
                    : coAuthorPair.Type);
        }
        
        return _commitMessageBuilder.Build();
    }

    public async Task<GitResult> ExecuteCommitAsync(CommitMessage commitMessage, CancellationToken cancellationToken)
        => await _gitClient.CommitAsync(commitMessage, cancellationToken);
}