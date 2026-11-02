using CoAttribution.Cli.Tests.Fakes;
using CoAttribution.Cli.Tests.Helpers;

namespace CoAttribution.Cli.Tests.Commands;

[NotInParallel]

public class AppConfigRegistryPathResolverTests
{
    [Test]
    public async Task GetGlobalRegistryPathAsync_WithGlobalRegistryInConfig_ReturnsPath()
    {
        string toml = """
                      [paths]
                      global_registry = "/tmp/registry"
                      """;
        using TempConfigFile tmp = new(toml);
        IConfiguration configuration =
            CommandTestHarness.SingleValueConfiguration("config-file", tmp.FilePath);

        AppConfigRegistryPathResolver resolver = new(configuration);

        string? path = await resolver.GetGlobalRegistryPathAsync(CancellationToken.None);

        await Assert.That(path).IsEqualTo("/tmp/registry");
    }

    [Test]
    public async Task GetGlobalRegistryPathAsync_WithoutConfigFileKey_ReturnsNull()
    {
        IConfiguration configuration =
            CommandTestHarness.SingleValueConfiguration("unrelated-key", "value");
        AppConfigRegistryPathResolver resolver = new(configuration);

        string? path = await resolver.GetGlobalRegistryPathAsync(CancellationToken.None);

        await Assert.That(path).IsNull();
    }

    [Test]
    public async Task GetGlobalRegistryPathAsync_WithMissingFile_ReturnsNull()
    {
        string missing = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"),
            "missing.toml");
        IConfiguration configuration =
            CommandTestHarness.SingleValueConfiguration("config-file", missing);
        AppConfigRegistryPathResolver resolver = new(configuration);

        string? path = await resolver.GetGlobalRegistryPathAsync(CancellationToken.None);

        await Assert.That(path).IsNull();
    }

    [Test]
    public async Task GetGlobalRegistryPathAsync_CachesResult()
    {
        string toml = """
                      [paths]
                      global_registry = "/tmp/registry"
                      """;
        using TempConfigFile tmp = new(toml);
        IConfiguration configuration =
            CommandTestHarness.SingleValueConfiguration("config-file", tmp.FilePath);

        AppConfigRegistryPathResolver resolver = new(configuration);

        string? first = await resolver.GetGlobalRegistryPathAsync(CancellationToken.None);
        await Assert.That(first).IsEqualTo("/tmp/registry");

        // Mutate the file to prove the resolver cached the value instead of re-reading.
        await File.WriteAllTextAsync(tmp.FilePath, "[paths]\nglobal_registry = \"/changed\"");

        string? second = await resolver.GetGlobalRegistryPathAsync(CancellationToken.None);
        await Assert.That(second).IsEqualTo("/tmp/registry");
    }

    [Test]
    public async Task GetGlobalRegistryPathAsync_WithKeyMissing_ReturnsNull()
    {
        using TempConfigFile tmp = new("[paths]\nother = \"value\"");
        IConfiguration configuration =
            CommandTestHarness.SingleValueConfiguration("config-file", tmp.FilePath);

        AppConfigRegistryPathResolver resolver = new(configuration);

        string? path = await resolver.GetGlobalRegistryPathAsync(CancellationToken.None);

        await Assert.That(path).IsNull();
    }

    [Test]
    public async Task GetExplicitRegistryPathAsync_WithRegistryPathKey_ReturnsValue()
    {
        IConfiguration configuration =
            CommandTestHarness.SingleValueConfiguration("registry-path", "/custom/registry/authors.toml");

        AppConfigRegistryPathResolver resolver = new(configuration);

        string? path = await resolver.GetExplicitRegistryPathAsync(CancellationToken.None);

        await Assert.That(path).IsEqualTo("/custom/registry/authors.toml");
    }

    [Test]
    public async Task GetExplicitRegistryPathAsync_WithoutRegistryPathKey_ReturnsNull()
    {
        using TempConfigFile tmp = new("[paths]\nglobal_registry = \"/tmp/registry\"");
        IConfiguration configuration =
            CommandTestHarness.SingleValueConfiguration("config-file", tmp.FilePath);

        AppConfigRegistryPathResolver resolver = new(configuration);

        string? path = await resolver.GetExplicitRegistryPathAsync(CancellationToken.None);

        await Assert.That(path).IsNull();
    }
}
