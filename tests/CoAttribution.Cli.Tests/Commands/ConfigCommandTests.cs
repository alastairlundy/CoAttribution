using CoAttribution.Cli.Tests.Fakes;
using CoAttribution.Cli.Tests.Helpers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CoAttribution.Cli.Tests.Commands;

[NotInParallel]

public class ConfigCommandTests
{
    [Test]
    public async Task GetValueAsync_WithPathKey_ReturnsValue()
    {
        IConfiguration configuration = CommandTestHarness.SingleValueConfiguration("config-file", "");
        IConfigResolver configResolver = Substitute.For<IConfigResolver>();
        configResolver.ResolveAppConfig(configuration, Arg.Any<CancellationToken>())
            .Returns(new AppConfig
            {
                PathsSettings = [],
                TrailersSettings = [],
                TuiSettings = [],
                AuthorsRegistry = new Dictionary<string, string> { ["paths.global"] = "/tmp/registry" }
            });

        ConfigCommand command = CommandTestHarness.BuildConfigCommand(configuration, configResolver);
        command.Key = "authors_registry.paths.global";
        command.Value = "";

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.GetValueAsync(CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(console.StandardOutput).Contains("/tmp/registry");
    }

    [Test]
    public async Task GetValueAsync_WithRemovedTrailersKeys_ReturnsInvalidKeyError()
    {
        // Even when the config still carries the stale values, get on a
        // removed key must report the invalid-key error, not the stale value.
        IConfiguration configuration = CommandTestHarness.SingleValueConfiguration("config-file", "");
        IConfigResolver configResolver = Substitute.For<IConfigResolver>();
        configResolver.ResolveAppConfig(configuration, Arg.Any<CancellationToken>())
            .Returns(new AppConfig
            {
                PathsSettings = [],
                TrailersSettings = new Dictionary<string, string>
                {
                    ["default"] = "Stale-Label",
                    ["assistance"] = "Stale-Label",
                    ["coauthorship"] = "Stale-Label"
                },
                TuiSettings = [],
                AuthorsRegistry = []
            });

        foreach (string key in new[] { "trailers.default", "trailers.assistance", "trailers.coauthorship" })
        {
            ConfigCommand command = CommandTestHarness.BuildConfigCommand(configuration, configResolver);
            command.Key = key;
            command.Value = "";

            using ConsoleCapture console = new();

            int exitCode = await command.GetValueAsync(CancellationToken.None);

            await Assert.That(exitCode).IsEqualTo(1);
            await Assert.That(console.StandardError).Contains($"Configuration Key '{key}' is not valid.");
            await Assert.That(console.StandardOutput).IsEmpty();
        }
    }

    [Test]
    public async Task SetValueAsync_WithRemovedTrailersKeys_ReturnsInvalidKeyErrorWithoutWriting()
    {
        string toml = """
                      [trailers]
                      default = "Stale-Label"
                      assistance = "Stale-Label"
                      coauthorship = "Stale-Label"
                      """;

        foreach (string key in new[] { "trailers.default", "trailers.assistance", "trailers.coauthorship" })
        {
            using TempConfigFile tmp = new(toml);
            string originalContents = await File.ReadAllTextAsync(tmp.FilePath);
            IConfiguration configuration =
                CommandTestHarness.SingleValueConfiguration("config-file", tmp.FilePath);
            IConfigResolver configResolver = Substitute.For<IConfigResolver>();
            configResolver.ResolveAppConfig(configuration, Arg.Any<CancellationToken>())
                .Returns(new AppConfig
                {
                    TrailersSettings = new Dictionary<string, string>
                    {
                        ["default"] = "Stale-Label",
                        ["assistance"] = "Stale-Label",
                        ["coauthorship"] = "Stale-Label"
                    }
                });

            ConfigCommand command = CommandTestHarness.BuildConfigCommand(configuration, configResolver);
            command.Key = key;
            command.Value = "hacked";

            using ConsoleCapture console = new();

            int exitCode = await command.SetValueAsync(CancellationToken.None);

            await Assert.That(exitCode).IsEqualTo(1);
            await Assert.That(console.StandardError).Contains($"Configuration Key '{key}' is not valid.");
            await Assert.That(await File.ReadAllTextAsync(tmp.FilePath)).IsEqualTo(originalContents);
        }
    }

    [Test]
    public async Task GetValueAsync_WithUnknownKey_ReturnsOne()
    {
        IConfiguration configuration = CommandTestHarness.SingleValueConfiguration("config-file", "");
        IConfigResolver configResolver = Substitute.For<IConfigResolver>();
        configResolver.ResolveAppConfig(configuration, Arg.Any<CancellationToken>())
            .Returns(new AppConfig());

        ConfigCommand command = CommandTestHarness.BuildConfigCommand(configuration, configResolver);
        command.Key = "totally.unknown.key";
        command.Value = "";

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.GetValueAsync(CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(console.StandardError).IsNotEmpty();
    }

    [Test]
    public async Task GetValueAsync_WhenResolverThrows_PropagatesException()
    {
        // Production awaits ResolveAppConfig OUTSIDE the try block, so the
        // exception is not caught by GetValueAsync. This test pins that
        // current behavior; if ConfigCommand is later refactored to handle
        // this case, update the test to assert the new return value.
        IConfiguration configuration = CommandTestHarness.SingleValueConfiguration("config-file", "");
        IConfigResolver configResolver = Substitute.For<IConfigResolver>();
        configResolver.ResolveAppConfig(configuration, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("corrupt"));

        ConfigCommand command = CommandTestHarness.BuildConfigCommand(configuration, configResolver);
        command.Key = "authors_registry.paths.global";
        command.Value = "";

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        await Assert.That(async () => await command.GetValueAsync(CancellationToken.None))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task SetValueAsync_WithTuiKey_WritesToConfigFile()
    {
        using TempConfigFile tmp = new(string.Empty);
        IConfiguration configuration = CommandTestHarness.SingleValueConfiguration("config-file", tmp.FilePath);
        IConfigResolver configResolver = Substitute.For<IConfigResolver>();
        configResolver.ResolveAppConfig(configuration, Arg.Any<CancellationToken>())
            .Returns(new AppConfig());

        ConfigCommand command = CommandTestHarness.BuildConfigCommand(configuration, configResolver);
        command.Key = "tui.color_scheme";
        command.Value = "dark";

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.SetValueAsync(CancellationToken.None);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(File.Exists(tmp.FilePath)).IsTrue();
        string contents = await File.ReadAllTextAsync(tmp.FilePath);
        await Assert.That(contents).Contains("color_scheme");
        await Assert.That(contents).Contains("dark");
    }
}
