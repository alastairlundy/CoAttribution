using CoAttribution.Cli.Tests.Fakes;
using CoAttribution.Cli.Tests.Helpers;

namespace CoAttribution.Cli.Tests.Commands;

[NotInParallel]

public class RootCommandTests
{
    [Test]
    public async Task RunAsync_ShowsHelpAndReturnsOne()
    {
        // In non-TUI builds (which is what these tests run in) the root
        // command prints help and returns 1.
        RootCommand command = CommandTestHarness.BuildRootCommand();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(1);
    }
}
