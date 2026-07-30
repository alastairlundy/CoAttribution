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
using NSubstitute;


namespace CoAttribution.Lib.Tests.CommitOrchestrator;

public class CommitOrchestratorTests
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

    private static GitCoAuthorConfig ConfigWith(params GitCoAuthor[] authors)
    {
        GitCoAuthorConfig config = new();
        foreach (GitCoAuthor a in authors)
        {
            if (a.Type == ContributorType.Agent)
            {
                config.Agents[a.CoAuthorId] = a;
            }
            else
            {
                config.Humans[a.CoAuthorId] = a;
            }
        }
        return config;
    }

    private static (ICommitMessageBuilder builder,
                    IAuthorRegistry registry,
                    IGitClient git,
                    IHostResolver hostResolver)
        MakeDeps(GitCoAuthorConfig config, HostResolutionResult hostResult, string? registryPath = null)
    {
        ICommitMessageBuilder builder = Substitute.For<ICommitMessageBuilder>();
        CommitMessage built = new("subject", ["body"], []);
        builder.SetContent(Arg.Any<string>(), Arg.Any<string>()).Returns(builder);
        builder.AddCoAuthors(Arg.Any<IEnumerable<ResolvedCoAuthor>>()).Returns(builder);
        builder.Build().Returns(built);

        IAuthorRegistry registry = Substitute.For<IAuthorRegistry>();
        registry.GetAuthorConfigAsync(Arg.Any<CancellationToken>()).Returns(config);
        registry.GetRegistryFileAsync(Arg.Any<CancellationToken>())
            .Returns(registryPath is null ? null : new FileInfo(registryPath));

        IGitClient git = Substitute.For<IGitClient>();
        git.CommitAsync(Arg.Any<CommitMessage>(), Arg.Any<CancellationToken>())
            .Returns(new GitResult(0, string.Empty, string.Empty));

        IHostResolver hostResolver = Substitute.For<IHostResolver>();
        hostResolver.ResolveHostAsync(Arg.Any<string?>()).Returns(hostResult);

        return (builder, registry, git, hostResolver);
    }

    [Test]
    public async Task BuildCommitMessage_NoHostResolved_BuildsSuccessfully()
    {
        GitCoAuthor alice = MakeAuthor("alice", ContributorType.Human, hasHostBlock: false);
        GitCoAuthorConfig config = ConfigWith(alice);

        (ICommitMessageBuilder builder, IAuthorRegistry registry, IGitClient git, IHostResolver hostResolver) =
            MakeDeps(config, new HostResolutionResult { Variant = HostResolutionVariant.NoHostDetected, Source = HostSource.Fallback });

        CoAttribution.Lib.CommitOrchestrator orchestrator = new(builder, registry, git, hostResolver);

        CommitRequest request = new("subject", "body", DefaultIds: [], CoAuthorIds: ["alice"], AssistIds: []);
        CommitMessage message = await orchestrator.BuildCommitMessageAsync(request, CancellationToken.None);

        await Assert.That(message.Subject).IsEqualTo("subject");
    }

    [Test]
    public async Task BuildCommitMessage_HostResolvedAllBlocksPresent_NoException()
    {
        GitCoAuthor copilot = MakeAuthor("copilot", ContributorType.Agent, hasHostBlock: true);
        GitCoAuthorConfig config = ConfigWith(copilot);

        (ICommitMessageBuilder builder, IAuthorRegistry registry, IGitClient git, IHostResolver hostResolver) =
            MakeDeps(
                config,
                new HostResolutionResult { Variant = HostResolutionVariant.Resolved, HostKey = "github", Source = HostSource.CliFlag },
                registryPath: "/tmp/authors.toml");

        CoAttribution.Lib.CommitOrchestrator orchestrator = new(builder, registry, git, hostResolver);

        CommitRequest request = new("subject", "body", DefaultIds: [], CoAuthorIds: ["copilot"], AssistIds: []);
        CommitMessage message = await orchestrator.BuildCommitMessageAsync(request, CancellationToken.None);

        await Assert.That(message.Subject).IsEqualTo("subject");
    }

    [Test]
    public async Task BuildCommitMessage_AgentMissingHostBlock_ThrowsDiagnosticException()
    {
        GitCoAuthor copilot = MakeAuthor("copilot", ContributorType.Agent, hasHostBlock: false);
        GitCoAuthorConfig config = ConfigWith(copilot);

        (ICommitMessageBuilder builder, IAuthorRegistry registry, IGitClient git, IHostResolver hostResolver) =
            MakeDeps(
                config,
                new HostResolutionResult { Variant = HostResolutionVariant.Resolved, HostKey = "github", Source = HostSource.CliFlag },
                registryPath: "/tmp/authors.toml");

        CoAttribution.Lib.CommitOrchestrator orchestrator = new(builder, registry, git, hostResolver);

        CommitRequest request = new("subject", "body", DefaultIds: [], CoAuthorIds: ["copilot"], AssistIds: []);

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
        await Assert.That(thrown!.Diagnostics).Count().IsEqualTo(1);
        await Assert.That(thrown.Diagnostics[0].HostKey).IsEqualTo("github");
        await Assert.That(thrown.Diagnostics[0].ContributorId).IsEqualTo("copilot");
        await Assert.That(thrown.Diagnostics[0].TomlSnippet).Contains("copilot");
    }

    [Test]
    public async Task BuildCommitMessage_HumanMissingHostBlock_DoesNotThrow()
    {
        // Humans are not subject to the per-host override requirement.
        GitCoAuthor alice = MakeAuthor("alice", ContributorType.Human, hasHostBlock: false);
        GitCoAuthorConfig config = ConfigWith(alice);

        (ICommitMessageBuilder builder, IAuthorRegistry registry, IGitClient git, IHostResolver hostResolver) =
            MakeDeps(
                config,
                new HostResolutionResult { Variant = HostResolutionVariant.Resolved, HostKey = "github", Source = HostSource.CliFlag },
                registryPath: "/tmp/authors.toml");

        CoAttribution.Lib.CommitOrchestrator orchestrator = new(builder, registry, git, hostResolver);

        CommitRequest request = new("subject", "body", DefaultIds: [], CoAuthorIds: ["alice"], AssistIds: []);

        CommitMessage message = await orchestrator.BuildCommitMessageAsync(request, CancellationToken.None);
        await Assert.That(message.Subject).IsEqualTo("subject");
    }

    [Test]
    public async Task ExecuteCommitAsync_DelegatesToGitClient()
    {
        (ICommitMessageBuilder builder, IAuthorRegistry registry, IGitClient git, IHostResolver hostResolver) =
            MakeDeps(
                new GitCoAuthorConfig(),
                new HostResolutionResult { Variant = HostResolutionVariant.NoHostDetected, Source = HostSource.Fallback });

        CoAttribution.Lib.CommitOrchestrator orchestrator = new(builder, registry, git, hostResolver);

        CommitMessage message = new("subject", ["body"], []);
        GitResult result = await orchestrator.ExecuteCommitAsync(message, CancellationToken.None);

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await git.Received(1).CommitAsync(Arg.Is(message), Arg.Any<CancellationToken>());
    }
}