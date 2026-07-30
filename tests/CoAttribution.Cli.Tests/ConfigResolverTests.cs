using CoAttribution.Cli.Tests.Fakes;
using CoAttribution.Cli.Tests.Helpers;

namespace CoAttribution.Cli.Tests.Commands;

[NotInParallel]

public class ConfigResolverTests
{
    [Test]
    public async Task ResolveAppConfig_WithValidFile_ReturnsParsedConfig()
    {
        string toml = """
                      [paths]
                      global_registry = "/tmp/registry"
                      """;
        using TempConfigFile tmp = new(toml);
        IConfiguration configuration =
            CommandTestHarness.SingleValueConfiguration("config-file", tmp.FilePath);

        ConfigResolver resolver = new();

        AppConfig config = await resolver.ResolveAppConfig(configuration, CancellationToken.None);

        await Assert.That(config.PathsSettings["global_registry"]).IsEqualTo("/tmp/registry");
    }

    [Test]
    public async Task ResolveAppConfig_WithoutConfigFileKey_Throws()
    {
        IConfiguration configuration =
            CommandTestHarness.SingleValueConfiguration("unrelated-key", "value");
        ConfigResolver resolver = new();

        await Assert.That(async () =>
                await resolver.ResolveAppConfig(configuration, CancellationToken.None))
            .Throws<FileNotFoundException>();
    }

    [Test]
    public async Task ResolveAppConfig_WithMissingFile_Throws()
    {
        string missing = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"),
            "does-not-exist.toml");
        IConfiguration configuration =
            CommandTestHarness.SingleValueConfiguration("config-file", missing);
        ConfigResolver resolver = new();

        await Assert.That(async () =>
                await resolver.ResolveAppConfig(configuration, CancellationToken.None))
            .Throws<FileNotFoundException>();
    }

    [Test]
    public async Task ResolveAppConfig_WithInvalidToml_Throws()
    {
        using TempConfigFile tmp = new("this is not = valid toml ====");
        IConfiguration configuration =
            CommandTestHarness.SingleValueConfiguration("config-file", tmp.FilePath);
        ConfigResolver resolver = new();

        await Assert.That(async () =>
                await resolver.ResolveAppConfig(configuration, CancellationToken.None))
            .Throws<Exception>();
    }
}
