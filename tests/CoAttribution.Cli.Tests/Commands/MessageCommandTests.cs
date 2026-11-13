using CoAttribution.Cli.Tests.Fakes;
using CoAttribution.Cli.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CoAttribution.Cli.Tests.Commands;

[NotInParallel]

public class MessageCommandTests
{
    /// <summary>
    /// A last-commit message carrying both managed trailers plus unowned
    /// content, so strip (flags present) and preserve (zero flags) are both
    /// observable in the preview output.
    /// </summary>
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

    /// <summary>
    /// A real orchestrator over faked registry/git/host dependencies: the
    /// preview and the commit amend path run through the exact rebuild the
    /// library ships, with the last commit's message read served from
    /// <paramref name="lastCommitRead"/>.
    /// </summary>
    private static (CommitOrchestrator Orchestrator, IGitClient Git) MakeAmendOrchestrator(
        GitCoAuthorConfig config, GitResult lastCommitRead)
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

        return (new CommitOrchestrator(builder, registry, git, hostResolver), git);
    }

    [Test]
    public async Task RunAsync_WhenOrchestratorSucceeds_WritesMessageAndReturnsZero()
    {
        ICommitOrchestrator orchestrator = Substitute.For<ICommitOrchestrator>();
        CommitMessage built = new("feat: new feature", ["body"], []);
        orchestrator.BuildCommitMessageAsync(Arg.Any<CommitRequest>(), Arg.Any<CancellationToken>())
            .Returns(built);

        MessageCommand command = CommandTestHarness.BuildMessageCommand(orchestrator);
        command.SubjectMessage = "feat: new feature";
        command.BodyMessage = "body";
        command.DefaultIds = ["copilot"];

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(console.StandardOutput).Contains("feat: new feature");
    }

    [Test]
    public async Task RunAsync_WhenUnknownCoAuthorException_ListsUnknownIdsAndReturnsOne()
    {
        ICommitOrchestrator orchestrator = Substitute.For<ICommitOrchestrator>();

        orchestrator.BuildCommitMessageAsync(Arg.Any<CommitRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new UnknownCoAuthorException(["coploit"], ["copilot", "kilo"]));

        MessageCommand command = CommandTestHarness.BuildMessageCommand(orchestrator);
        command.SubjectMessage = "feat: x";
        command.DefaultIds = [];

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(console.StandardError).Contains("coploit");
        await Assert.That(console.StandardError).Contains("copilot");
    }

    [Test]
    public async Task RunAsync_WhenGenericException_FormatsFailureMessageWithSubject()
    {
        ICommitOrchestrator orchestrator = Substitute.For<ICommitOrchestrator>();
        orchestrator.BuildCommitMessageAsync(Arg.Any<CommitRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        MessageCommand command = CommandTestHarness.BuildMessageCommand(orchestrator);
        command.SubjectMessage = "feat: add telemetry";
        command.DefaultIds = [];
        command.Verbose = false;

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(console.StandardError).Contains("feat: add telemetry");
        await Assert.That(console.StandardError).DoesNotContain("{0}");
    }

    [Test]
    public async Task RunAsync_WhenMissingHostBlockException_FormatsDiagnosticsAndReturnsOne()
    {
        ICommitOrchestrator orchestrator = Substitute.For<ICommitOrchestrator>();

        MissingHostBlockDiagnostic diag = new(
            HostKey: "github",
            ContributorId: "copilot",
            RegistryPath: "/tmp/authors.toml",
            TomlSnippet: "[host.github]\nname = \"...\"\nemail = \"...\"");

        orchestrator.BuildCommitMessageAsync(Arg.Any<CommitRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new MissingHostBlockException([diag]));

        MessageCommand command = CommandTestHarness.BuildMessageCommand(orchestrator);
        command.SubjectMessage = "feat: x";
        command.DefaultIds = ["copilot"];

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(console.StandardError).IsNotEmpty();
        await Assert.That(console.StandardError).Contains("github");
    }

    [Test]
    public async Task RunAsync_WhenGenericException_AndVerboseFalse_WritesFailureAndReturnsOne()
    {
        ICommitOrchestrator orchestrator = Substitute.For<ICommitOrchestrator>();
        orchestrator.BuildCommitMessageAsync(Arg.Any<CommitRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        MessageCommand command = CommandTestHarness.BuildMessageCommand(orchestrator);
        command.SubjectMessage = "feat: x";
        command.DefaultIds = ["copilot"];
        command.Verbose = false;

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(console.StandardError).IsNotEmpty();
        await Assert.That(console.StandardError).DoesNotContain("boom");
    }

    [Test]
    public async Task RunAsync_WhenGenericException_AndVerboseTrue_AppendsExceptionMessage()
    {
        ICommitOrchestrator orchestrator = Substitute.For<ICommitOrchestrator>();
        orchestrator.BuildCommitMessageAsync(Arg.Any<CommitRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        MessageCommand command = CommandTestHarness.BuildMessageCommand(orchestrator);
        command.SubjectMessage = "feat: x";
        command.DefaultIds = ["copilot"];
        command.Verbose = true;

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(console.StandardError).Contains("boom");
    }

    // ---------- amend preview ----------

    [Test]
    public async Task RunAsync_AmendWithAttributionFlag_PreviewMatchesCommitAmendRebuildWithoutCommitting()
    {
        GitCoAuthor alice = MakeAuthor("alice", ContributorType.Human);

        // What `git commit --amend` would receive for the same invocation.
        (CommitOrchestrator commitOrchestrator, IGitClient commitGit) =
            MakeAmendOrchestrator(ConfigWith(alice), new GitResult(0, LastCommitMessage, string.Empty));

        CommitMessage? amended = null;
        commitGit.AmendCommitAsync(Arg.Any<CommitMessage>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                amended = callInfo.Arg<CommitMessage>();
                return new GitResult(0, string.Empty, string.Empty);
            });

        await commitOrchestrator.AmendCommitAsync(
            new CommitRequest("Ignored subject", "Ignored body",
                DefaultIds: [], CoAuthorIds: ["alice"], AssistIds: [], Amend: true),
            CancellationToken.None);

        // The preview runs the same read and rebuild through a second,
        // identical orchestrator and prints it instead of committing.
        (CommitOrchestrator previewOrchestrator, IGitClient previewGit) =
            MakeAmendOrchestrator(ConfigWith(alice), new GitResult(0, LastCommitMessage, string.Empty));

        MessageCommand command = CommandTestHarness.BuildMessageCommand(previewOrchestrator);
        command.Amend = true;
        command.CoAuthorIds = ["alice"];

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(amended).IsNotNull();

        // The preview prints exactly what the commit amend path would commit.
        await Assert.That(console.StandardOutput).Contains(amended!.ToConsoleFormat());

        // Strip-and-apply: the previous managed tokens are gone, unowned
        // content survives, and the current invocation's trailer is applied.
        await Assert.That(console.StandardOutput).DoesNotContain("Co-authored-by: Old Author");
        await Assert.That(console.StandardOutput).DoesNotContain("Assisted-by: Old Agent");
        await Assert.That(console.StandardOutput).Contains("Co-authored-by: alice <alice@example.com>");
        await Assert.That(console.StandardOutput).Contains("Signed-off-by: Signer <signer@example.com>");

        // Same last-commit read as the commit amend path — and only a read:
        // no commit execution is reachable from the preview.
        await previewGit.Received(1).GetLastCommitMessageAsync(Arg.Any<CancellationToken>());
        await previewGit.DidNotReceive().CommitAsync(Arg.Any<CommitMessage>(), Arg.Any<CancellationToken>());
        await previewGit.DidNotReceive().AmendCommitAsync(Arg.Any<CommitMessage>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RunAsync_AmendWithZeroAttributionFlags_PreviewPreservesManagedTokensAndMatchesCommitAmendRebuild()
    {
        // What `git commit --amend` with zero attribution flags would receive.
        (CommitOrchestrator commitOrchestrator, IGitClient commitGit) =
            MakeAmendOrchestrator(new GitCoAuthorConfig(), new GitResult(0, LastCommitMessage, string.Empty));

        CommitMessage? amended = null;
        commitGit.AmendCommitAsync(Arg.Any<CommitMessage>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                amended = callInfo.Arg<CommitMessage>();
                return new GitResult(0, string.Empty, string.Empty);
            });

        await commitOrchestrator.AmendCommitAsync(
            new CommitRequest("Ignored subject", "Ignored body",
                DefaultIds: [], CoAuthorIds: [], AssistIds: [], Amend: true),
            CancellationToken.None);

        (CommitOrchestrator previewOrchestrator, IGitClient previewGit) =
            MakeAmendOrchestrator(new GitCoAuthorConfig(), new GitResult(0, LastCommitMessage, string.Empty));

        MessageCommand command = CommandTestHarness.BuildMessageCommand(previewOrchestrator);
        command.Amend = true;

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(amended).IsNotNull();

        // The preview prints exactly what the commit amend path would commit.
        await Assert.That(console.StandardOutput).Contains(amended!.ToConsoleFormat());

        // Zero attribution flags: no strip runs, so the previous managed
        // tokens are preserved in the preview output.
        await Assert.That(console.StandardOutput).Contains("Co-authored-by: Old Author <old@example.com>");
        await Assert.That(console.StandardOutput).Contains("Assisted-by: Old Agent <agent@example.com>");
        await Assert.That(console.StandardOutput).Contains("Signed-off-by: Signer <signer@example.com>");

        await previewGit.Received(1).GetLastCommitMessageAsync(Arg.Any<CancellationToken>());
        await previewGit.DidNotReceive().CommitAsync(Arg.Any<CommitMessage>(), Arg.Any<CancellationToken>());
        await previewGit.DidNotReceive().AmendCommitAsync(Arg.Any<CommitMessage>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RunAsync_AmendPreview_BuildsWithAmendFlagAndNeverInvokesCommitExecution()
    {
        ICommitOrchestrator orchestrator = Substitute.For<ICommitOrchestrator>();
        CommitMessage built = new("rebuild: from head", ["body"], []);
        orchestrator.BuildCommitMessageAsync(Arg.Any<CommitRequest>(), Arg.Any<CancellationToken>())
            .Returns(built);

        MessageCommand command = CommandTestHarness.BuildMessageCommand(orchestrator);
        command.Amend = true;
        command.DefaultIds = ["copilot"];

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(console.StandardOutput).Contains("rebuild: from head");

        await orchestrator.Received(1).BuildCommitMessageAsync(
            Arg.Is<CommitRequest>(request => request.Amend), Arg.Any<CancellationToken>());
        await orchestrator.DidNotReceive().ExecuteCommitAsync(Arg.Any<CommitMessage>(), Arg.Any<CancellationToken>());
        await orchestrator.DidNotReceive().AmendCommitAsync(Arg.Any<CommitRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RunAsync_AmendBareInvocation_ParsesAndPrintsPreviewWithoutCommitting()
    {
        // The shipped command line: `co-attr message --amend` alone must
        // parse (neither --message nor --with is required for a preview),
        // print the rebuilt message, and never reach a commit.
        ICommitOrchestrator orchestrator = Substitute.For<ICommitOrchestrator>();
        orchestrator.BuildCommitMessageAsync(Arg.Any<CommitRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CommitMessage("rebuild: from head", ["body"], []));

        ServiceCollection services = new();
        services.AddSingleton(orchestrator);
        using ServiceProvider provider = services.BuildServiceProvider();

        System.CommandLine.RootCommand root = RootCommand.Create(provider);

        using ConsoleCapture console = new();
        int exitCode = await CliRunner.RunAsync(root, ["message", "--amend"]);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(console.StandardOutput).Contains("rebuild: from head");

        await orchestrator.Received(1).BuildCommitMessageAsync(
            Arg.Is<CommitRequest>(request => request.Amend
                && request.DefaultIds.Length == 0
                && request.CoAuthorIds.Length == 0
                && request.AssistIds.Length == 0),
            Arg.Any<CancellationToken>());
        await orchestrator.DidNotReceive().ExecuteCommitAsync(Arg.Any<CommitMessage>(), Arg.Any<CancellationToken>());
        await orchestrator.DidNotReceive().AmendCommitAsync(Arg.Any<CommitRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RunAsync_AmendPreviewWithoutPriorCommit_SurfacesAmendErrorAndReturnsOne()
    {
        ICommitOrchestrator orchestrator = Substitute.For<ICommitOrchestrator>();
        orchestrator.BuildCommitMessageAsync(Arg.Any<CommitRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AmendWithoutCommitException("fatal: your current branch 'main' does not have any commits yet"));

        MessageCommand command = CommandTestHarness.BuildMessageCommand(orchestrator);
        command.Amend = true;

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(console.StandardError).Contains("Cannot amend");
        await Assert.That(console.StandardError).Contains("does not have any commits yet");
    }
}
