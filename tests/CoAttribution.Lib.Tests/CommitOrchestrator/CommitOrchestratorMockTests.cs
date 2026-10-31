/*
    CoAttribution.Lib.Tests
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Lib.Builders;
using CoAttribution.Lib.Exceptions;
using CoAttribution.Lib.HostResolution;
using CoAttribution.Lib.Models.DTOs;
using TUnit.Mocks;
using TUnit.Mocks.Generated;
using static TUnit.Mocks.Arguments.Arg;

namespace CoAttribution.Lib.Tests.CommitOrchestrator;

/// <summary>
/// TUnit.Mocks-based regression tests for orchestrator allocation/lookup
/// hot paths. Kept in a dedicated file so the NSubstitute-based suite files
/// are unaffected by the TUnit.Mocks generated namespace.
/// </summary>
public class CommitOrchestratorMockTests
{
    private static GitCoAuthor MakeAuthor(string id, ContributorType type, bool hasHostBlock = true)
    {
        GitCoAuthor author = new()
        {
            CoAuthorId = id,
            Name = id,
            Email = $"{id}@example.com",
            Type = type,
        };
        if (hasHostBlock)
        {
            author.Host["github"] = new HostOverride { Name = id + " GH", Email = $"{id}@gh.com" };
        }
        return author;
    }

    [Test]
    public async Task BuildCommitMessage_MultipleAgentsMissingHostBlock_QueriesRegistryFileOnce()
    {
        // The registry file lookup must be hoisted out of the per-author loop:
        // two agents missing a host block should still trigger exactly one
        // registry file resolution, not one per offending agent.
        GitCoAuthor copilot = MakeAuthor("copilot", ContributorType.Agent, hasHostBlock: false);
        GitCoAuthor jeeves = MakeAuthor("jeeves", ContributorType.Agent, hasHostBlock: false);

        GitCoAuthorConfig config = new();
        config.Agents[copilot.CoAuthorId] = copilot;
        config.Agents[jeeves.CoAuthorId] = jeeves;

        var builderMock = ICommitMessageBuilder.Mock();
        builderMock.Build()
            .Returns(new CommitMessage("subject", ["body"], []));

        var registryMock = IAuthorRegistry.Mock();
        registryMock.GetAuthorConfigAsync(Any<CancellationToken>())
            .Returns(config);
        registryMock.GetRegistryFileAsync(Any<CancellationToken>())
            .Returns(new FileInfo("/tmp/authors.toml"));

        var gitMock = IGitClient.Mock();
        gitMock.CommitAsync(Any<CommitMessage>(), Any<CancellationToken>())
            .Returns(new GitResult(0, string.Empty, string.Empty));

        var hostResolverMock = IHostResolver.Mock();
        hostResolverMock.ResolveHostAsync(Any<string?>())
            .Returns(new HostResolutionResult
            {
                Variant = HostResolutionVariant.Resolved,
                HostKey = "github",
                Source = HostSource.CliFlag
            });

        CoAttribution.Lib.CommitOrchestrator orchestrator =
            new(builderMock, registryMock, gitMock, hostResolverMock);

        CommitRequest request = new("subject", "body",
            DefaultIds: [], CoAuthorIds: ["copilot", "jeeves"], AssistIds: []);

        MissingHostBlockException? thrown = null;
        try
        {
            await orchestrator.BuildCommitMessageAsync(request, CancellationToken.None);
        }
        catch (MissingHostBlockException ex)
        {
            thrown = ex;
        }

        await Assert.That(thrown).IsNotNull();
        await Assert.That(thrown!.Diagnostics).Count().IsEqualTo(2);

        registryMock.GetRegistryFileAsync(Any<CancellationToken>())
            .WasCalled(Times.Once);
    }
}
