using CoAttribution.Cli.Tests.Fakes;
using CoAttribution.Cli.Tests.Helpers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CoAttribution.Cli.Tests.Commands;

[NotInParallel]

public class RemoveCoAuthorCommandTests
{
    [Test]
    public async Task RunAsync_WhenRegistrySucceeds_ReturnsZero()
    {
        IAuthorRegistry registry = Substitute.For<IAuthorRegistry>();
        registry.GetRegistryFileAsync(Arg.Any<CancellationToken>())
            .Returns(new FileInfo(Path.GetTempFileName()));
        registry.RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        RemoveCoAuthorCommand command = CommandTestHarness.BuildRemoveCommand(registry);
        command.Ids = ["copilot"];
        command.Verbose = false;

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
        registry.RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("not found"));

        RemoveCoAuthorCommand command = CommandTestHarness.BuildRemoveCommand(registry);
        command.Ids = ["missing"];
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
        registry.RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("not found"));

        RemoveCoAuthorCommand command = CommandTestHarness.BuildRemoveCommand(registry);
        command.Ids = ["missing"];
        command.Verbose = true;

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(console.StandardError).Contains("not found");
    }

    [Test]
    public async Task RunAsync_PassesAllIdsToRegistry()
    {
        IAuthorRegistry registry = Substitute.For<IAuthorRegistry>();
        registry.GetRegistryFileAsync(Arg.Any<CancellationToken>())
            .Returns(new FileInfo(Path.GetTempFileName()));
        registry.RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        RemoveCoAuthorCommand command = CommandTestHarness.BuildRemoveCommand(registry);
        command.Ids = ["copilot", "claude", "opencode"];

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        _ = await command.RunAsync(ctx);

        await registry.Received(1).RemoveAsync(
            Arg.Is<string[]>(ids =>
                ids.Length == 3 &&
                ids.Contains("copilot") &&
                ids.Contains("claude") &&
                ids.Contains("opencode")),
            Arg.Any<CancellationToken>());
    }
}
