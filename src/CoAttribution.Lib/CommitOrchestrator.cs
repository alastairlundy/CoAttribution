/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Lib.Builders;
using CoAttribution.Lib.Exceptions;
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
        
        ResolvedCoAuthor[] actualCoAuthors = AttributionPolicy.Resolve(new CoAuthorResolutionRequest(
            coAuthors, mergedDefaultIds,
            commitRequest.CoAuthorIds, commitRequest.AssistIds));

        if (hostResult.Variant == HostResolutionVariant.Resolved && hostResult.HostKey is not null)
        {
            List<MissingHostBlockDiagnostic> missingBlocks = [];

            foreach (ResolvedCoAuthor resolved in actualCoAuthors)
            {
                GitCoAuthor author = resolved.Author;

                if (author.Type != ContributorType.Agent)
                    continue;

                if (author.Host.ContainsKey(hostResult.HostKey))
                    continue;

                FileInfo? registryFile = await _authorRegistry.GetRegistryFileAsync(cancellationToken);
                string registryPath = registryFile?.FullName ?? "N/A";
                string section = author.Type == ContributorType.Agent ? "agents" : "humans";
                string snippet = $"[{section}.{author.CoAuthorId}.host.{hostResult.HostKey}]\nname = \"{author.Name}\"\nemail = \"{author.Email}\"";

                missingBlocks.Add(new MissingHostBlockDiagnostic(
                    hostResult.HostKey,
                    author.CoAuthorId,
                    registryPath,
                    snippet));
            }

            if (missingBlocks.Count > 0)
            {
                throw new MissingHostBlockException(missingBlocks.AsReadOnly());
            }
        }

        _commitMessageBuilder.AddCoAuthors(actualCoAuthors);
        
        return _commitMessageBuilder.Build();
    }

    public async Task<GitResult> ExecuteCommitAsync(CommitMessage commitMessage, CancellationToken cancellationToken)
        => await _gitClient.CommitAsync(commitMessage, cancellationToken);
}