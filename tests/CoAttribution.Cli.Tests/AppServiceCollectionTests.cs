using CoAttribution.Cli.Tests.Fakes;
using CoAttribution.Cli.Tests.Helpers;
using CoAttribution.Cli.Tui.ViewModels;
using CoAttribution.Cli.Tui.Views;
using Microsoft.Extensions.DependencyInjection;

namespace CoAttribution.Cli.Tests;

/// <summary>
/// Resolution coverage for the production container built by
/// <see cref="AppServiceCollection.AddCoAttributionServices"/>.
/// </summary>
/// <remarks>
/// These tests pin the wiring that <c>commit</c>, <c>message</c>, and the TUI
/// depend on: <see cref="AppConfig"/> is a constructor dependency of
/// <see cref="HostResolver"/>, and an unregistered service there surfaces only
/// at resolution time as an <see cref="InvalidOperationException"/>, so each
/// graph is resolved end-to-end rather than asserted descriptor-by-descriptor.
/// <para>Marked <c>NotInParallel</c>: MainWindow resolution constructs
/// Terminal.Gui views, like the other TUI view tests.</para>
/// </remarks>
[NotInParallel]
public class AppServiceCollectionTests
{
    private static ServiceProvider BuildServices(string configFilePath) =>
        new ServiceCollection()
            .AddCoAttributionServices(CommandTestHarness.SingleValueConfiguration("config-file", configFilePath))
            .BuildServiceProvider();

    private static string MissingConfigFilePath() =>
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "config.toml");

    // ---------- the graphs that failed before AppConfig was registered ----------

    [Test]
    public async Task HostResolver_ResolutionGraph_Resolves()
    {
        using ServiceProvider services = BuildServices(MissingConfigFilePath());

        IHostResolver resolver = services.GetRequiredService<IHostResolver>();

        await Assert.That(resolver).IsNotNull();
    }

    [Test]
    public async Task CommitAndMessage_ResolutionGraph_Resolves()
    {
        // commit and message both activate CommitCommand/MessageCommand with
        // ICommitOrchestrator, which pulls in IHostResolver.
        using ServiceProvider services = BuildServices(MissingConfigFilePath());

        ICommitOrchestrator orchestrator = services.GetRequiredService<ICommitOrchestrator>();

        await Assert.That(orchestrator).IsNotNull();
    }

    [Test]
    public async Task TuiAuthorSelection_ResolutionGraph_Resolves()
    {
        // The TUI's AuthorSelectionViewModel is built from IAuthorRegistry and
        // IHostResolver; MainWindow pulls the rest of the view graph in.
        using ServiceProvider services = BuildServices(MissingConfigFilePath());

        AuthorSelectionViewModel viewModel = services.GetRequiredService<AuthorSelectionViewModel>();
        MainWindow mainWindow = services.GetRequiredService<MainWindow>();

        await Assert.That(viewModel).IsNotNull();
        await Assert.That(mainWindow).IsNotNull();
    }

    // ---------- AppConfig loading semantics ----------

    [Test]
    public async Task AppConfig_WithMissingConfigFile_FallsBackToDefaults()
    {
        using ServiceProvider services = BuildServices(MissingConfigFilePath());

        AppConfig appConfig = services.GetRequiredService<AppConfig>();

        await Assert.That(appConfig).IsNotNull();
        await Assert.That(appConfig.HostAliases.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AppConfig_WithConfigFile_LoadsHostAliases()
    {
        using TempConfigFile tmp = new("""
                                       [host_aliases]
                                       "github.example.com" = "internal"
                                       """);
        using ServiceProvider services = BuildServices(tmp.FilePath);

        AppConfig appConfig = services.GetRequiredService<AppConfig>();

        await Assert.That(appConfig.HostAliases["github.example.com"]).IsEqualTo("internal");
    }
}
