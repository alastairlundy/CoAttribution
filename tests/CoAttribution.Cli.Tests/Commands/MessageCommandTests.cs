using CoAttribution.Cli.Tests.Fakes;
using CoAttribution.Cli.Tests.Helpers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CoAttribution.Cli.Tests.Commands;

[NotInParallel]

public class MessageCommandTests
{
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
}
