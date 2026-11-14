using CoAttribution.Cli.Tests.Fakes;
using CoAttribution.Cli.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace CoAttribution.Cli.Tests;

/// <summary>
/// Coverage for the trailer config key removal and migration warning: the
/// removed keys are rejected by the CLI surface, a populated <c>[trailers]</c>
/// section produces exactly one stderr warning line per process run without
/// ever touching the file, and stale trailer values never affect output.
/// </summary>
[NotInParallel]
public class TrailerConfigMigrationTests
{
    private const string StaleTrailersConfig = """
        [paths]
        global_registry = "/tmp/registry"

        [trailers]
        default = "Stale-Label"
        assistance = "Stale-Label"
        coauthorship = "Stale-Label"
        """;

    private static readonly string[] RemovedKeys =
        ["trailers.default", "trailers.assistance", "trailers.coauthorship"];

    // ---------- the removed keys are rejected by the CLI surface ----------

    [Test]
    public async Task ConfigCommand_WithRemovedTrailersKeys_ReportsInvalidKeyError()
    {
        foreach (string key in RemovedKeys)
        {
            (int exitCode, _, string error) = await RunCli("config", key, "value");

            await Assert.That(exitCode).IsEqualTo(1);
            await Assert.That(error).Contains(key);
            await Assert.That(error).Contains("not recognized");
        }
    }

    [Test]
    public async Task ConfigGetStyleInvocation_WithRemovedKey_ReportsError()
    {
        // The pinned surface has no get/set subcommands, so `config get <key>`
        // parses `get` as the key and is rejected at parse time with a
        // non-zero exit code.
        (int exitCode, _, string error) = await RunCli("config", "get", "trailers.assistance");

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(error).IsNotEmpty();
    }

    // ---------- one warning line per run across every config read path ----------

    [Test]
    public async Task ConfigReads_WithPopulatedTrailersSection_PrintExactlyOneWarningLine()
    {
        using TempConfigFile tmp = new(StaleTrailersConfig);
        IConfiguration configuration =
            CommandTestHarness.SingleValueConfiguration("config-file", tmp.FilePath);
        ConfigResolver resolver = new();
        AppConfigRegistryPathResolver registryPathResolver = new(configuration);

        using ConsoleCapture console = new();

        // The same file is read several times per real run: the config
        // command resolves it, the registry-path resolver reads it, and the
        // DI AppConfig factory (commit/message/TUI) reads it again.
        _ = await resolver.ResolveAppConfig(configuration, CancellationToken.None);
        _ = await resolver.ResolveAppConfig(configuration, CancellationToken.None);
        _ = await registryPathResolver.GetGlobalRegistryPathAsync(CancellationToken.None);
        using ServiceProvider services = new ServiceCollection()
            .AddCoAttributionServices(configuration)
            .BuildServiceProvider();
        _ = services.GetRequiredService<AppConfig>();

        string[] warningLines = console.StandardError
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        await Assert.That(warningLines.Length).IsEqualTo(1);
        await Assert.That(warningLines[0]).Contains(tmp.FilePath);
        await Assert.That(warningLines[0]).Contains("ignored");
        await Assert.That(console.StandardOutput).IsEmpty();
    }

    [Test]
    public async Task ResolveAppConfig_WithoutTrailersSection_DoesNotWarn()
    {
        using TempConfigFile tmp = new("""
                                       [paths]
                                       global_registry = "/tmp/registry"
                                       """);
        IConfiguration configuration =
            CommandTestHarness.SingleValueConfiguration("config-file", tmp.FilePath);
        ConfigResolver resolver = new();

        using ConsoleCapture console = new();

        _ = await resolver.ResolveAppConfig(configuration, CancellationToken.None);

        await Assert.That(console.StandardError).IsEmpty();
    }

    [Test]
    public async Task ResolveAppConfig_WithEmptyTrailersSection_DoesNotWarn()
    {
        // Detection is for a non-empty section: an empty [trailers] table
        // carries no values and must stay silent.
        using TempConfigFile tmp = new("""
                                       [paths]
                                       global_registry = "/tmp/registry"

                                       [trailers]
                                       """);
        IConfiguration configuration =
            CommandTestHarness.SingleValueConfiguration("config-file", tmp.FilePath);
        ConfigResolver resolver = new();

        using ConsoleCapture console = new();

        _ = await resolver.ResolveAppConfig(configuration, CancellationToken.None);

        await Assert.That(console.StandardError).IsEmpty();
    }

    // ---------- the file is never rewritten ----------

    [Test]
    public async Task ResolveAppConfig_WithPopulatedTrailersSection_DoesNotModifyFile()
    {
        using TempConfigFile tmp = new(StaleTrailersConfig);
        string originalContents = await File.ReadAllTextAsync(tmp.FilePath);
        IConfiguration configuration =
            CommandTestHarness.SingleValueConfiguration("config-file", tmp.FilePath);
        ConfigResolver resolver = new();

        using ConsoleCapture console = new();

        _ = await resolver.ResolveAppConfig(configuration, CancellationToken.None);
        _ = await resolver.ResolveAppConfig(configuration, CancellationToken.None);

        await Assert.That(await File.ReadAllTextAsync(tmp.FilePath)).IsEqualTo(originalContents);
    }

    [Test]
    public async Task SetValueAsync_OnValidKey_PreservesTrailersSection()
    {
        using TempConfigFile tmp = new(StaleTrailersConfig);
        string originalContents = await File.ReadAllTextAsync(tmp.FilePath);
        IConfiguration configuration =
            CommandTestHarness.SingleValueConfiguration("config-file", tmp.FilePath);

        ConfigCommand command = CommandTestHarness.BuildConfigCommand(configuration, new ConfigResolver());
        command.Key = "authors_registry.paths.global";
        command.Value = "/tmp/new-registry";

        using ConsoleCapture console = new();

        int exitCode = await command.SetValueAsync(CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(0);

        // The set rewrites the file for its own key but round-trips the
        // stale [trailers] section instead of auto-cleaning it.
        string updatedContents = await File.ReadAllTextAsync(tmp.FilePath);
        await Assert.That(updatedContents).Contains("Stale-Label");
        await Assert.That(updatedContents).Contains("new-registry");
        await Assert.That(originalContents).Contains("Stale-Label");
    }

    // ---------- trailer output is unchanged by stale config values ----------

    [Test]
    public async Task TrailerOutput_WithStaleTrailersConfig_UsesLocalizedTokensOnly()
    {
        using TempConfigFile tmp = new(StaleTrailersConfig);
        IConfiguration configuration =
            CommandTestHarness.SingleValueConfiguration("config-file", tmp.FilePath);

        using ConsoleCapture console = new();
        AppConfig config = await new ConfigResolver().ResolveAppConfig(configuration, CancellationToken.None);

        // The stale values load but are never consulted for trailer text.
        await Assert.That(config.TrailersSettings["default"]).IsEqualTo("Stale-Label");

        CommitMessage message = new(
            Subject: "subject",
            BodyLines: [],
            CoAuthors:
            [
                new ResolvedCoAuthor(
                    new GitCoAuthor { CoAuthorId = "a", Name = "Alice", Email = "alice@example.com" },
                    AttributionType.CoAuthor),
                new ResolvedCoAuthor(
                    new GitCoAuthor { CoAuthorId = "b", Name = "Bot", Email = "bot@example.com" },
                    AttributionType.Assisted)
            ]);

        (string _, string trailer) = message.ToGitFormat();

        await Assert.That(trailer).Contains("Co-authored-by: Alice <alice@example.com>");
        await Assert.That(trailer).Contains("Assisted-by: Bot <bot@example.com>");
        await Assert.That(trailer).DoesNotContain("Stale-Label");
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCli(params string[] args)
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(CommandTestHarness.SingleValueConfiguration("config-file", ""));
        services.AddSingleton(Substitute.For<IConfigResolver>());
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        System.CommandLine.RootCommand root = RootCommand.Create(serviceProvider);

        using ConsoleCapture console = new();
        int exitCode = await CliRunner.RunAsync(root, args);

        return (exitCode, console.StandardOutput, console.StandardError);
    }
}
