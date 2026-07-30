using CoAttribution.Cli.Tests.Fakes;
using CoAttribution.Cli.Tests.Helpers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CoAttribution.Cli.Tests.Commands;

[NotInParallel]

public class CommitCommandTests
{
    [Test]
    public async Task RunAsync_WhenOrchestratorSucceeds_ReturnsGitResultExitCode()
    {
        ICommitOrchestrator orchestrator = Substitute.For<ICommitOrchestrator>();
        orchestrator.BuildCommitMessageAsync(Arg.Any<CommitRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CommitMessage("feat: x", [], []));
        orchestrator.ExecuteCommitAsync(Arg.Any<CommitMessage>(), Arg.Any<CancellationToken>())
            .Returns(new GitResult(0, "ok", ""));

        CommitCommand command = CommandTestHarness.BuildCommitCommand(orchestrator);
        command.SubjectMessage = "feat: x";
        command.DefaultIds = ["copilot"];

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task RunAsync_WhenOrchestratorReturnsNonZero_PropagatesExitCode()
    {
        ICommitOrchestrator orchestrator = Substitute.For<ICommitOrchestrator>();
        orchestrator.BuildCommitMessageAsync(Arg.Any<CommitRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CommitMessage("feat: x", [], []));
        orchestrator.ExecuteCommitAsync(Arg.Any<CommitMessage>(), Arg.Any<CancellationToken>())
            .Returns(new GitResult(7, "", "pre-commit hook failed"));

        CommitCommand command = CommandTestHarness.BuildCommitCommand(orchestrator);
        command.SubjectMessage = "feat: x";
        command.DefaultIds = ["copilot"];

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(7);
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

        CommitCommand command = CommandTestHarness.BuildCommitCommand(orchestrator);
        command.SubjectMessage = "feat: x";
        command.DefaultIds = ["copilot"];

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(console.StandardError).Contains("github");
    }

    [Test]
    public async Task RunAsync_WhenGenericException_AndVerboseFalse_ReturnsOne()
    {
        ICommitOrchestrator orchestrator = Substitute.For<ICommitOrchestrator>();
        orchestrator.BuildCommitMessageAsync(Arg.Any<CommitRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        CommitCommand command = CommandTestHarness.BuildCommitCommand(orchestrator);
        command.SubjectMessage = "feat: x";
        command.DefaultIds = ["copilot"];
        command.Verbose = false;

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(console.StandardError).DoesNotContain("boom");
    }

    [Test]
    public async Task RunAsync_WhenGenericException_AndVerboseTrue_AppendsExceptionMessage()
    {
        ICommitOrchestrator orchestrator = Substitute.For<ICommitOrchestrator>();
        orchestrator.BuildCommitMessageAsync(Arg.Any<CommitRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        CommitCommand command = CommandTestHarness.BuildCommitCommand(orchestrator);
        command.SubjectMessage = "feat: x";
        command.DefaultIds = ["copilot"];
        command.Verbose = true;

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(console.StandardError).Contains("boom");
    }
}
