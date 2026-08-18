using CoAttribution.Cli.Tests.Fakes;
using CoAttribution.Cli.Tests.Helpers;
using NSubstitute;

namespace CoAttribution.Cli.Tests.Commands;

[NotInParallel]

public class InitCommandTests
{
    [Test]
    public async Task RunAsync_WithConfigFilePath_CreatesConfigFile()
    {
        string tempDir = Path.Combine(
            Path.GetTempPath(),
            "coattribution-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            string configPath = Path.Combine(tempDir, "config.toml");
            await Assert.That(File.Exists(configPath)).IsFalse();

            IConfiguration configuration =
                CommandTestHarness.SingleValueConfiguration("config-file", configPath);
            IRegistryPathResolver pathResolver = Substitute.For<IRegistryPathResolver>();
            pathResolver.GetGlobalRegistryPathAsync(Arg.Any<CancellationToken>())
                .Returns((string?)null);

            InitCommand command = CommandTestHarness.BuildInitCommand(configuration, pathResolver);
            command.CreateGlobalFile = true;

            using ConsoleCapture console = new();
            CliContext ctx = CliContextFactory.Create();

            int exitCode = await command.RunAsync(ctx);

            await Assert.That(exitCode).IsEqualTo(0);
            await Assert.That(File.Exists(configPath)).IsTrue();
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    [Test]
    public async Task RunAsync_WithEmptyPath_UsesConfigurationValue()
    {
        string tempDir = Path.Combine(
            Path.GetTempPath(),
            "coattribution-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            string configPath = Path.Combine(tempDir, "config.toml");

            IConfiguration configuration =
                CommandTestHarness.SingleValueConfiguration("config-file", configPath);
            IRegistryPathResolver pathResolver = Substitute.For<IRegistryPathResolver>();
            pathResolver.GetGlobalRegistryPathAsync(Arg.Any<CancellationToken>())
                .Returns((string?)null);

            InitCommand command = CommandTestHarness.BuildInitCommand(configuration, pathResolver);
            command.CreateGlobalFile = true;

            using ConsoleCapture console = new();
            CliContext ctx = CliContextFactory.Create();

            int exitCode = await command.RunAsync(ctx);

            await Assert.That(exitCode).IsEqualTo(0);
            await Assert.That(File.Exists(configPath)).IsTrue();
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    [Test]
    public async Task RunAsync_SkipsConfigFileCreation_WhenFileExists()
    {
        using TempConfigFile existing = new("# pre-existing config");
        IConfiguration configuration =
            CommandTestHarness.SingleValueConfiguration("config-file", existing.FilePath);
        IRegistryPathResolver pathResolver = Substitute.For<IRegistryPathResolver>();
        pathResolver.GetGlobalRegistryPathAsync(Arg.Any<CancellationToken>())
            .Returns((string?)null);

        InitCommand command = CommandTestHarness.BuildInitCommand(configuration, pathResolver);
        command.CreateGlobalFile = true;

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();

        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(0);
        string contents = await File.ReadAllTextAsync(existing.FilePath);
        await Assert.That(contents).IsEqualTo("# pre-existing config");
    }

    [Test]
    public async Task RunAsync_WithLocalFile_UsesLocalAuthorsFilePath()
    {
        string tempDir = Path.Combine(
            Path.GetTempPath(),
            "coattribution-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            string configPath = Path.Combine(tempDir, "config.toml");
            IConfiguration configuration =
                CommandTestHarness.SingleValueConfiguration("config-file", configPath);
            IRegistryPathResolver pathResolver = Substitute.For<IRegistryPathResolver>();
            pathResolver.GetGlobalRegistryPathAsync(Arg.Any<CancellationToken>())
                .Returns((string?)null);

            // InitCommand places the local authors file under
            // <Environment.CurrentDirectory>/.coauthor/ regardless of where the
            // config file lives. Change the working directory for the duration
            // of this test.
            string originalCwd = Environment.CurrentDirectory;
            Environment.CurrentDirectory = tempDir;
            try
            {
                InitCommand command =
                    CommandTestHarness.BuildInitCommand(configuration, pathResolver);
                command.CreateGlobalFile = false;

                using ConsoleCapture console = new();
                CliContext ctx = CliContextFactory.Create();

                int exitCode = await command.RunAsync(ctx);

                await Assert.That(exitCode).IsEqualTo(0);
                string localAuthorsPath = Path.Combine(
                    tempDir, ".coauthor", "authors.toml");
                await Assert.That(File.Exists(localAuthorsPath)).IsTrue();
            }
            finally
            {
                Environment.CurrentDirectory = originalCwd;
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }
}
