using CoAttribution.Cli.Tests.Fakes;
using CoAttribution.Cli.Tests.Helpers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CoAttribution.Cli.Tests.Commands;

[NotInParallel]

public class AddCoAuthorCommandTests
{
    [Test]
    public async Task RunAsync_WithAgentType_AddsAsAgentAndReturnsZero()
    {
        IAuthorRegistry registry = Substitute.For<IAuthorRegistry>();
        registry.GetRegistryFileAsync(Arg.Any<CancellationToken>())
            .Returns(new FileInfo(Path.GetTempFileName()));
        registry.AddAsync(Arg.Any<GitCoAuthor>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        AddCoAuthorCommand command = CommandTestHarness.BuildAddCommand(registry);
        command.Id = "copilot";
        command.AuthorType = "agent";
        command.AuthorName = "GitHub Copilot";
        command.AuthorEmail = "copilot@github.com";

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(console.StandardError).IsEmpty();
    }

    [Test]
    public async Task RunAsync_WithHumanType_AddsAsHuman()
    {
        IAuthorRegistry registry = Substitute.For<IAuthorRegistry>();
        registry.GetRegistryFileAsync(Arg.Any<CancellationToken>())
            .Returns(new FileInfo(Path.GetTempFileName()));
        registry.AddAsync(Arg.Any<GitCoAuthor>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        AddCoAuthorCommand command = CommandTestHarness.BuildAddCommand(registry);
        command.Id = "alice";
        command.AuthorType = "human";
        command.AuthorName = "Alice Example";
        command.AuthorEmail = "alice@example.com";

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(console.StandardError).IsEmpty();
    }

    [Test]
    public async Task RunAsync_WhenRegistryThrows_ReturnsOneAndWritesError()
    {
        IAuthorRegistry registry = Substitute.For<IAuthorRegistry>();
        registry.GetRegistryFileAsync(Arg.Any<CancellationToken>())
            .Returns(new FileInfo(Path.GetTempFileName()));
        registry.AddAsync(Arg.Any<GitCoAuthor>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("disk full"));

        AddCoAuthorCommand command = CommandTestHarness.BuildAddCommand(registry);
        command.Id = "bob";
        command.AuthorType = "human";
        command.AuthorName = "Bob";
        command.AuthorEmail = "bob@example.com";
        command.Verbose = false;

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(console.StandardError).IsNotEmpty();
    }

    [Test]
    public async Task RunAsync_WhenRegistryThrows_AndVerboseIsTrue_AppendsExceptionMessage()
    {
        IAuthorRegistry registry = Substitute.For<IAuthorRegistry>();
        registry.GetRegistryFileAsync(Arg.Any<CancellationToken>())
            .Returns(new FileInfo(Path.GetTempFileName()));
        registry.AddAsync(Arg.Any<GitCoAuthor>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("disk full"));

        AddCoAuthorCommand command = CommandTestHarness.BuildAddCommand(registry);
        command.Id = "bob";
        command.AuthorType = "human";
        command.AuthorName = "Bob";
        command.AuthorEmail = "bob@example.com";
        command.Verbose = true;

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(console.StandardError).Contains("disk full");
    }

    [Test]
    public async Task RunAsync_PassesCancellationTokenToRegistry()
    {
        IAuthorRegistry registry = Substitute.For<IAuthorRegistry>();
        registry.GetRegistryFileAsync(Arg.Any<CancellationToken>())
            .Returns(new FileInfo(Path.GetTempFileName()));
        registry.AddAsync(Arg.Any<GitCoAuthor>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        AddCoAuthorCommand command = CommandTestHarness.BuildAddCommand(registry);
        command.Id = "copilot";
        command.AuthorType = "agent";
        command.AuthorName = "GitHub Copilot";
        command.AuthorEmail = "copilot@github.com";

        using CancellationTokenSource cts = new();
        CliContext ctx = CliContextFactory.Create(cts.Token);

        _ = await command.RunAsync(ctx);

        await registry.Received(1).AddAsync(
            Arg.Is<GitCoAuthor>(a => a != null && a.CoAuthorId == "copilot" && a.Type == ContributorType.Agent),
            cts.Token);
    }
}
