/*
    CoAttribution.Lib.Tests
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Lib.Builders;
using CoAttribution.Lib.Models.DTOs;
using NSubstitute;

namespace CoAttribution.Lib.Tests.CommitOrchestrator;

/// <summary>
/// Covers the amend pipeline: last-commit read, managed-token strip per the
/// flags-present rule, rebuild with the current invocation's trailers, amend
/// execution, and the no-prior-commit failure mode.
/// </summary>
public class CommitOrchestratorAmendTests
{
    private const string LastCommitMessage =
        "Subject line\n" +
        "\n" +
        "Body paragraph.\n" +
        "\n" +
        "Co-authored-by: Old Author <old@example.com>\n" +
        "Assisted-by: Old Agent <agent@example.com>\n" +
        "Signed-off-by: Signer <signer@example.com>\n" +
        "Generated-with: CoAttribution\n";

    private static GitCoAuthor MakeAuthor(string id, ContributorType type)
    {
        return new GitCoAuthor
        {
            CoAuthorId = id,
            Name = id,
            Email = $"{id}@example.com",
            Type = type,
        };
    }

    private static GitCoAuthorConfig ConfigWith(params GitCoAuthor[] authors)
    {
        GitCoAuthorConfig config = new();
        foreach (GitCoAuthor author in authors)
        {
            if (author.Type == ContributorType.Agent)
            {
                config.Agents[author.CoAuthorId] = author;
            }
            else
            {
                config.Humans[author.CoAuthorId] = author;
            }
        }
        return config;
    }

    private static (CoAttribution.Lib.CommitOrchestrator orchestrator,
                    IAuthorRegistry registry,
                    IGitClient git)
        MakeOrchestrator(GitCoAuthorConfig config, GitResult lastCommitRead)
    {
        CommitMessageBuilder builder = new();

        IAuthorRegistry registry = Substitute.For<IAuthorRegistry>();
        registry.GetAuthorConfigAsync(Arg.Any<CancellationToken>()).Returns(config);

        IGitClient git = Substitute.For<IGitClient>();
        git.GetLastCommitMessageAsync(Arg.Any<CancellationToken>()).Returns(lastCommitRead);
        git.AmendCommitAsync(Arg.Any<CommitMessage>(), Arg.Any<CancellationToken>())
            .Returns(new GitResult(0, string.Empty, string.Empty));

        IHostResolver hostResolver = Substitute.For<IHostResolver>();
        hostResolver.ResolveHostAsync(Arg.Any<string?>())
            .Returns(new HostResolutionResult
            {
                Variant = HostResolutionVariant.NoHostDetected,
                Source = HostSource.Fallback,
            });

        CoAttribution.Lib.CommitOrchestrator orchestrator =
            new(builder, registry, git, hostResolver);

        return (orchestrator, registry, git);
    }

    [Test]
    public async Task AmendCommitAsync_AttributionFlagPresent_StripsManagedTrailersAndAppliesCurrentOnes()
    {
        GitCoAuthor alice = MakeAuthor("alice", ContributorType.Human);
        (CoAttribution.Lib.CommitOrchestrator orchestrator, _, IGitClient git) =
            MakeOrchestrator(ConfigWith(alice), new GitResult(0, LastCommitMessage, string.Empty));

        CommitRequest request = new("Ignored subject", "Ignored body",
            DefaultIds: [], CoAuthorIds: ["alice"], AssistIds: [], Amend: true);

        CommitMessage? amended = null;
        git.AmendCommitAsync(Arg.Any<CommitMessage>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                amended = callInfo.Arg<CommitMessage>();
                return new GitResult(0, string.Empty, string.Empty);
            });

        GitResult result = await orchestrator.AmendCommitAsync(request, CancellationToken.None);

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await git.Received(1).GetLastCommitMessageAsync(Arg.Any<CancellationToken>());
        await git.Received(1).AmendCommitAsync(Arg.Any<CommitMessage>(), Arg.Any<CancellationToken>());

        await Assert.That(amended).IsNotNull();

        (string message, string trailer) = amended!.ToGitFormat();

        // The previous message's content survives apart from the managed
        // trailer lines, which the strip removed for this invocation.
        await Assert.That(message.StartsWith("Subject line", StringComparison.Ordinal)).IsTrue();
        await Assert.That(message.Contains("Body paragraph.")).IsTrue();
        await Assert.That(message.Contains("Co-authored-by: Old Author")).IsFalse();
        await Assert.That(message.Contains("Assisted-by: Old Agent")).IsFalse();
        await Assert.That(message.Contains("Signed-off-by: Signer <signer@example.com>")).IsTrue();
        await Assert.That(message.Contains("Generated-with: CoAttribution")).IsTrue();

        // The current invocation's trailer replaces the stripped ones.
        await Assert.That(trailer.Contains("Co-authored-by: alice <alice@example.com>")).IsTrue();
        await Assert.That(trailer.Contains("Assisted-by:")).IsFalse();
    }

    [Test]
    public async Task AmendCommitAsync_ZeroAttributionFlags_PreservesExistingManagedTrailers()
    {
        (CoAttribution.Lib.CommitOrchestrator orchestrator, _, IGitClient git) =
            MakeOrchestrator(new GitCoAuthorConfig(), new GitResult(0, LastCommitMessage, string.Empty));

        CommitRequest request = new("Ignored subject", "Ignored body",
            DefaultIds: [], CoAuthorIds: [], AssistIds: [], Amend: true);

        CommitMessage? amended = null;
        git.AmendCommitAsync(Arg.Any<CommitMessage>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                amended = callInfo.Arg<CommitMessage>();
                return new GitResult(0, string.Empty, string.Empty);
            });

        GitResult result = await orchestrator.AmendCommitAsync(request, CancellationToken.None);

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await git.Received(1).GetLastCommitMessageAsync(Arg.Any<CancellationToken>());

        await Assert.That(amended).IsNotNull();

        (string message, string trailer) = amended!.ToGitFormat();

        // Zero attribution flags: no strip runs, so the previous managed
        // tokens pass through unchanged and no new trailers are appended.
        await Assert.That(message.Contains("Co-authored-by: Old Author <old@example.com>")).IsTrue();
        await Assert.That(message.Contains("Assisted-by: Old Agent <agent@example.com>")).IsTrue();
        await Assert.That(message.Contains("Signed-off-by: Signer <signer@example.com>")).IsTrue();
        await Assert.That(string.IsNullOrEmpty(trailer)).IsTrue();
    }

    [Test]
    public async Task AmendCommitAsync_NoPriorCommit_SurfacesUserReadableAmendError()
    {
        GitResult readFailure = new(128, string.Empty,
            "fatal: your current branch 'main' does not have any commits yet");

        (CoAttribution.Lib.CommitOrchestrator orchestrator, IAuthorRegistry registry, IGitClient git) =
            MakeOrchestrator(new GitCoAuthorConfig(), readFailure);

        CommitRequest request = new("Ignored subject", "Ignored body",
            DefaultIds: [], CoAuthorIds: [], AssistIds: [], Amend: true);

        AmendWithoutCommitException? thrown = null;
        try
        {
            await orchestrator.AmendCommitAsync(request, CancellationToken.None);
        }
        catch (AmendWithoutCommitException ex)
        {
            thrown = ex;
        }

        await Assert.That(thrown).IsNotNull();
        await Assert.That(thrown!.Message).Contains("Cannot amend");
        await Assert.That(thrown.Message).Contains("does not have any commits yet");

        // The failure happens on the read: no resolution, no amend execution.
        await git.Received(1).GetLastCommitMessageAsync(Arg.Any<CancellationToken>());
        await git.DidNotReceive().AmendCommitAsync(Arg.Any<CommitMessage>(), Arg.Any<CancellationToken>());
        await registry.DidNotReceive().GetAuthorConfigAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task BuildCommitMessageAsync_AmendFlag_ReturnsRebuiltMessageWithoutExecuting()
    {
        // The preview-facing rebuild shares this path: it must produce the
        // same rebuilt message while never reaching git's commit execution.
        GitCoAuthor alice = MakeAuthor("alice", ContributorType.Human);
        (CoAttribution.Lib.CommitOrchestrator orchestrator, _, IGitClient git) =
            MakeOrchestrator(ConfigWith(alice), new GitResult(0, LastCommitMessage, string.Empty));

        CommitRequest request = new("Ignored subject", "Ignored body",
            DefaultIds: [], CoAuthorIds: ["alice"], AssistIds: [], Amend: true);

        CommitMessage message = await orchestrator.BuildCommitMessageAsync(request, CancellationToken.None);

        await git.Received(1).GetLastCommitMessageAsync(Arg.Any<CancellationToken>());
        await git.DidNotReceive().CommitAsync(Arg.Any<CommitMessage>(), Arg.Any<CancellationToken>());
        await git.DidNotReceive().AmendCommitAsync(Arg.Any<CommitMessage>(), Arg.Any<CancellationToken>());

        (string text, string trailer) = message.ToGitFormat();

        await Assert.That(text.Contains("Co-authored-by: Old Author")).IsFalse();
        await Assert.That(text.Contains("Signed-off-by: Signer <signer@example.com>")).IsTrue();
        await Assert.That(trailer.Contains("Co-authored-by: alice <alice@example.com>")).IsTrue();
    }
}
