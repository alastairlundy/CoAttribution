/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Lib.Builders;
using CoAttribution.Lib.HostResolution;
using CoAttribution.Lib.HostResolution.Abstractions;

namespace CoAttribution.Lib;

public class CommitOrchestrator : ICommitOrchestrator
{
    private readonly ICommitMessageBuilder _commitMessageBuilder;
    private readonly IAuthorRegistry _authorRegistry;
    private readonly IGitClient _gitClient;
    private readonly IHostResolver _hostResolver;

    public CommitOrchestrator(ICommitMessageBuilder commitMessageBuilder,
        IAuthorRegistry authorRegistry,
        IGitClient gitClient,
        IHostResolver hostResolver)
    {
        _commitMessageBuilder = commitMessageBuilder;
        _authorRegistry = authorRegistry;
        _gitClient = gitClient;
        _hostResolver = hostResolver;
    }
    
    public async Task<CommitMessage> BuildCommitMessageAsync(CommitRequest commitRequest, CancellationToken cancellationToken)
    {
        _commitMessageBuilder.SetContent(commitRequest.MessageSubject, commitRequest.MessageBody);

        GitCoAuthorConfig authorConfig = await _authorRegistry.GetAuthorConfigAsync(cancellationToken);

        GitCoAuthor[] coAuthors = authorConfig.GetCoAuthors();

        HostResolutionResult hostResult = await _hostResolver.ResolveHostAsync(null);
        string[] mergedDefaultIds = hostResult.Variant == HostResolutionVariant.Resolved && hostResult.HostKey is not null
            ? [..commitRequest.DefaultIds, hostResult.HostKey]
            : commitRequest.DefaultIds;
        
        ResolvedCoAuthor[] actualCoAuthors = AttributionPolicy.Resolve(
            coAuthors, mergedDefaultIds,
            commitRequest.CoAuthorIds, commitRequest.AssistIds);
        
        _commitMessageBuilder.AddCoAuthors(actualCoAuthors);
        
        return _commitMessageBuilder.Build();
    }

    public async Task<GitResult> ExecuteCommitAsync(CommitMessage commitMessage, CancellationToken cancellationToken)
        => await _gitClient.CommitAsync(commitMessage, cancellationToken);
}