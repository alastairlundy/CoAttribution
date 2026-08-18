using CoAttribution.Cli.Tests.Fakes;
using CoAttribution.Cli.Tests.Helpers;
using CoAttribution.Cli.Tui.Composition;
using CoAttribution.Lib.Abstractions;

namespace CoAttribution.Cli.Tests.Commands;

[NotInParallel]

public class RootCommandTests
{
    [Test]
    public async Task RunAsync_ShowsHelpAndReturnsZero()
    {
        // In non-TUI builds (which is what these tests run in) the root
        // command detects redirected I/O and prints help, returning 0.
        IAuthorRegistry registry = Substitute.For<IAuthorRegistry>();
        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        TuiCompositionRoot compositionRoot = new(serviceProvider);
        RootCommand command = CommandTestHarness.BuildRootCommand(registry, compositionRoot);
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(0);
    }
}
