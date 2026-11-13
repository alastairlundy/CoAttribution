using CoAttribution.Cli.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace CoAttribution.Cli.Tests.Commands;

/// <summary>
/// End-to-end tests that drive the real parse/invoke pipeline over the
/// command tree built by <c>RootCommand.Create</c>. The tree is built against
/// an empty service provider so the parse/help paths can be verified without
/// a fully configured application container; the goal is to verify the
/// parse/parse-failure/help paths return the expected exit codes from the
/// command line surface.
/// </summary>
[NotInParallel]
public class ProgramEntryPointTests
{
    [Test]
    public async Task RunAsync_WithNoArgs_ReturnsNonZero()
    {
        using ConsoleCapture console = new();

        // The service provider is deliberately empty: the root action
        // resolves RootCommand (which needs IAuthorRegistry etc.) and that
        // failure is caught by the default exception handler and returned
        // as a non-zero exit code rather than thrown as an exception.
        int exitCode = await RunWithEmptyServices(Array.Empty<string>());

        await Assert.That(exitCode).IsNotEqualTo(0);
    }

    [Test]
    public async Task RunAsync_WithUnknownSubcommand_ReturnsNonZero()
    {
        using ConsoleCapture console = new();

        int exitCode = await RunWithEmptyServices(["definitely-not-a-real-subcommand"]);

        await Assert.That(exitCode).IsNotEqualTo(0);
    }

    [Test]
    public async Task RunAsync_WithHelpFlag_ReturnsZero()
    {
        // The CLI surface returns 0 when the help text is rendered
        // successfully via --help.
        using ConsoleCapture console = new();

        int exitCode = await RunWithEmptyServices(["--help"]);

        await Assert.That(exitCode).IsEqualTo(0);
    }

    private static async Task<int> RunWithEmptyServices(string[] args)
    {
        using ServiceProvider serviceProvider = new ServiceCollection().BuildServiceProvider();
        System.CommandLine.RootCommand rootCommand = RootCommand.Create(serviceProvider);

        return await CliRunner.RunAsync(rootCommand, args);
    }
}
