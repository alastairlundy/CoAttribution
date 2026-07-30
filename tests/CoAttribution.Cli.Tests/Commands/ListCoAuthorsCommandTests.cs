using System.Text.Json;
using CoAttribution.Cli.Tests.Fakes;
using CoAttribution.Cli.Tests.Helpers;
using NSubstitute;

namespace CoAttribution.Cli.Tests.Commands;

[NotInParallel]

public class ListCoAuthorsCommandTests
{
    [Test]
    public async Task RunAsync_WithDefaultType_AndTextFormat_WritesAllAuthorsAsLines()
    {
        IAuthorRegistry registry = Substitute.For<IAuthorRegistry>();
        registry.GetAuthorConfigAsync(Arg.Any<CancellationToken>())
            .Returns(AuthorConfigFactory.WithAgentAndHuman());

        ListCoAuthorsCommand command = CommandTestHarness.BuildListCommand(registry);
        command.AuthorType = "";
        command.Format = "text";

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(console.StandardOutput).Contains("Alice Example");
        await Assert.That(console.StandardOutput).Contains("GitHub Copilot");
    }

    [Test]
    public async Task RunAsync_WithAgentTypeFilter_AndTextFormat_WritesOnlyAgents()
    {
        IAuthorRegistry registry = Substitute.For<IAuthorRegistry>();
        registry.GetAuthorConfigAsync(Arg.Any<CancellationToken>())
            .Returns(AuthorConfigFactory.WithAgentAndHuman());

        ListCoAuthorsCommand command = CommandTestHarness.BuildListCommand(registry);
        command.AuthorType = "agent";
        command.Format = "text";

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(console.StandardOutput).Contains("GitHub Copilot");
        await Assert.That(console.StandardOutput).DoesNotContain("Alice Example");
    }

    [Test]
    public async Task RunAsync_WithHumanTypeFilter_AndTextFormat_WritesOnlyHumans()
    {
        IAuthorRegistry registry = Substitute.For<IAuthorRegistry>();
        registry.GetAuthorConfigAsync(Arg.Any<CancellationToken>())
            .Returns(AuthorConfigFactory.WithAgentAndHuman());

        ListCoAuthorsCommand command = CommandTestHarness.BuildListCommand(registry);
        command.AuthorType = "human";
        command.Format = "text";

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(console.StandardOutput).Contains("Alice Example");
        await Assert.That(console.StandardOutput).DoesNotContain("GitHub Copilot");
    }

    [Test]
    public async Task RunAsync_WithJsonFormat_WritesValidJson()
    {
        IAuthorRegistry registry = Substitute.For<IAuthorRegistry>();
        registry.GetAuthorConfigAsync(Arg.Any<CancellationToken>())
            .Returns(AuthorConfigFactory.WithAgentAndHuman());

        ListCoAuthorsCommand command = CommandTestHarness.BuildListCommand(registry);
        command.AuthorType = "";
        command.Format = "json";

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(0);

        // The command writes one JSON array as a single line.
        string trimmed = console.StandardOutput.Trim();
        GitCoAuthor[]? parsed = JsonSerializer.Deserialize<GitCoAuthor[]>(trimmed,
            CoAuthorJsonContext.Default.GitCoAuthorArray);
        await Assert.That(parsed).IsNotNull();
        await Assert.That(parsed!.Length).IsEqualTo(2);
    }

    [Test]
    public async Task RunAsync_WithEmptyConfig_AndTextFormat_WritesNothingAndReturnsZero()
    {
        IAuthorRegistry registry = Substitute.For<IAuthorRegistry>();
        registry.GetAuthorConfigAsync(Arg.Any<CancellationToken>())
            .Returns(AuthorConfigFactory.Empty());

        ListCoAuthorsCommand command = CommandTestHarness.BuildListCommand(registry);
        command.AuthorType = "";
        command.Format = "text";

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(console.StandardOutput.Trim()).IsEmpty();
    }
}
