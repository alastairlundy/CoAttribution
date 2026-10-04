using CoAttribution.Cli.Tests.Fakes;
using CoAttribution.Cli.Tests.Helpers;
using CoAttribution.Cli.Tui.Composition;
using CoAttribution.Lib.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using CoAttribution.Cli.Tui.Dialogs;

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
        ILogger<TuiCompositionRoot> compositionLogger = NullLogger<TuiCompositionRoot>.Instance;
        TuiCompositionRoot compositionRoot = new(serviceProvider, compositionLogger);
        SetupDialog setupDialog = new(registry);
        RootCommand command = CommandTestHarness.BuildRootCommand(registry, compositionRoot, setupDialog);
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(0);
    }
}
