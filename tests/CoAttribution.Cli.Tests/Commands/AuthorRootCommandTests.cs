using CoAttribution.Cli.Tests.Fakes;
using CoAttribution.Cli.Tests.Helpers;

namespace CoAttribution.Cli.Tests.Commands;

[NotInParallel]

public class AuthorRootCommandTests
{
    [Test]
    public async Task RunAsync_ShowsHelpAndReturnsOne()
    {
        AuthorRootCommand command = CommandTestHarness.BuildAuthorRootCommand();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(1);
    }
}
