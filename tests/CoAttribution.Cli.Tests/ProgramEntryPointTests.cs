using CoAttribution.Cli.Tests.Fakes;

namespace CoAttribution.Cli.Tests.Commands;

/// <summary>
/// End-to-end tests that drive the real <c>Cli.RunAsync&lt;RootCommand&gt;</c>
/// entry point. The test executable reuses the same Program entry point, so
/// the goal is to verify the parse/parse-failure/help paths return the
/// expected exit codes from the command line surface.
/// </summary>
[NotInParallel]
public class ProgramEntryPointTests
{
    [Test]
    public async Task RunAsync_WithNoArgs_ReturnsNonZero()
    {
        using ConsoleCapture console = new();

        int exitCode = await DotMake.CommandLine.Cli.RunAsync<RootCommand>(
            Array.Empty<string>(),
            new DotMake.CommandLine.CliSettings { EnableDefaultExceptionHandler = false },
            CancellationToken.None);

        await Assert.That(exitCode).IsNotEqualTo(0);
    }

    [Test]
    public async Task RunAsync_WithUnknownSubcommand_ReturnsNonZero()
    {
        using ConsoleCapture console = new();

        int exitCode = await DotMake.CommandLine.Cli.RunAsync<RootCommand>(
            new[] { "definitely-not-a-real-subcommand" },
            new DotMake.CommandLine.CliSettings { EnableDefaultExceptionHandler = false },
            CancellationToken.None);

        await Assert.That(exitCode).IsNotEqualTo(0);
    }

    [Test]
    public async Task RunAsync_WithHelpFlag_ReturnsZero()
    {
        // The DotMake CLI surface returns 0 when the help text is rendered
        // successfully via --help.
        using ConsoleCapture console = new();

        int exitCode = await DotMake.CommandLine.Cli.RunAsync<RootCommand>(
            new[] { "--help" },
            new DotMake.CommandLine.CliSettings { EnableDefaultExceptionHandler = false },
            CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(0);
    }
}
