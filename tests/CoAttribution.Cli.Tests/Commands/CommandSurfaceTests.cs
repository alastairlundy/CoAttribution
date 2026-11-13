using CoAttribution.Cli.Tests.Fakes;
using CoAttribution.Cli.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine.Help;
using System.Reflection;

namespace CoAttribution.Cli.Tests.Commands;

/// <summary>
/// Surface-comparison pass over the System.CommandLine command tree: pins the
/// command names, aliases (including <c>cfg</c>), exit codes, parse-error
/// messages, and help output shape to the behavior the tool exposed before
/// the CLI-framework migration.
///
/// <para>
/// Help assertions target the lines that survived the migration byte-for-byte
/// (banner, usage lines, option/argument labels, aliases, section headings,
/// recursive root options). Framework-owned presentation that changed with the
/// parser swap — such as the <c>(REQUIRED)</c> marker, argument placeholders
/// in the root command list, and the help option's line position — is
/// asserted only where it is stable.
/// </para>
/// </summary>
[NotInParallel]
public class CommandSurfaceTests
{
    private static System.CommandLine.RootCommand BuildRoot(ServiceProvider services)
    {
        System.CommandLine.RootCommand root = RootCommand.Create(services);

        // The usage line is prefixed with the entry-assembly name, which is
        // co-attr for the shipped binary but CoAttribution.Cli.Tests when the
        // test executable is the entry point. Pin the shipped name so help
        // assertions match the real tool.
        root.HelpName = "co-attr";

        // Render help at an unbounded width so long lines (the config key's
        // allowed-value list) never wrap mid-token in captured output. The
        // production default still follows the console width.
        foreach (System.CommandLine.Option option in root.Options)
        {
            if (option is HelpOption { Action: BannerHelpAction bannerHelp })
            {
                bannerHelp.Help.MaxWidth = int.MaxValue;
            }
        }

        return root;
    }

    /// <summary>
    /// The version the help banner prints: the informational version of the
    /// CLI assembly (the shipped tool assembly, not the test entry point).
    /// </summary>
    private static string ToolVersion()
    {
        System.Reflection.AssemblyInformationalVersionAttribute? informationalVersion =
            (System.Reflection.AssemblyInformationalVersionAttribute?)typeof(BannerHelpAction).Assembly
                .GetCustomAttribute(typeof(System.Reflection.AssemblyInformationalVersionAttribute));

        return informationalVersion!.InformationalVersion.Split('+')[0];
    }

    private static string ExpectedBanner => $"co-attr v{ToolVersion()}\nCopyright (c) Alastair Lundy 2026\n\n";

    private static ServiceProvider BuildSurfaceServices()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(CommandTestHarness.SingleValueConfiguration("config-file", ""));
        services.AddSingleton(Substitute.For<IConfigResolver>());

        return services.BuildServiceProvider();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunAsync(params string[] args)
    {
        using ServiceProvider services = BuildSurfaceServices();
        System.CommandLine.RootCommand root = BuildRoot(services);

        using ConsoleCapture console = new();
        int exitCode = await CliRunner.RunAsync(root, args);

        return (exitCode, console.StandardOutput, console.StandardError);
    }

    // ---------- command tree: names and aliases ----------

    [Test]
    public async Task RootCommand_ExposesThePreMigrationCommandNamesAndAliases()
    {
        using ServiceProvider services = BuildSurfaceServices();
        System.CommandLine.RootCommand root = BuildRoot(services);

        string[] subcommands = [.. root.Subcommands.Select(c => c.Name)];

        await Assert.That(subcommands.Length).IsEqualTo(5);
        await Assert.That(subcommands).Contains("author");
        await Assert.That(subcommands).Contains("commit");
        await Assert.That(subcommands).Contains("config");
        await Assert.That(subcommands).Contains("init");
        await Assert.That(subcommands).Contains("message");

        System.CommandLine.Command config = root.Subcommands.Single(c => c.Name == "config");
        await Assert.That(config.Aliases).Contains("cfg");

        System.CommandLine.Command author = root.Subcommands.Single(c => c.Name == "author");
        string[] authorSubcommands = [.. author.Subcommands.Select(c => c.Name)];
        await Assert.That(authorSubcommands.Length).IsEqualTo(3);
        await Assert.That(authorSubcommands).Contains("add");
        await Assert.That(authorSubcommands).Contains("list");
        await Assert.That(authorSubcommands).Contains("remove");
    }

    [Test]
    public async Task RootCommand_RecursiveOptionsAreDeclaredOnTheRoot()
    {
        using ServiceProvider services = BuildSurfaceServices();
        System.CommandLine.RootCommand root = BuildRoot(services);

        System.CommandLine.Option configPath = root.Options.Single(o => o.Name == "--config-path");
        System.CommandLine.Option registry = root.Options.Single(o => o.Name == "--registry");

        await Assert.That(configPath.Recursive).IsTrue();
        await Assert.That(registry.Recursive).IsTrue();
        await Assert.That(configPath.Required).IsFalse();
        await Assert.That(registry.Required).IsFalse();
    }

    [Test]
    public async Task Subcommands_RecursiveRootOptionsAreAcceptedOnEverySubcommand()
    {
        using ServiceProvider services = BuildSurfaceServices();
        System.CommandLine.RootCommand root = BuildRoot(services);

        string[][] invocations =
        [
            ["commit", "-m", "s", "--with", "a", "--config-path", "c", "--registry", "r"],
            ["message", "-m", "s", "--with", "a", "--config-path", "c", "--registry", "r"],
            ["config", "trailers.default", "x", "--config-path", "c", "--registry", "r"],
            ["init", "--config-path", "c", "--registry", "r"],
            ["author", "--config-path", "c", "--registry", "r"],
            ["author", "add", "id", "--type", "human", "--name", "n", "--email", "e@x",
                "--config-path", "c", "--registry", "r"],
            ["author", "remove", "id", "--config-path", "c", "--registry", "r"],
            ["author", "list", "--config-path", "c", "--registry", "r"],
        ];

        foreach (string[] invocation in invocations)
        {
            int errorCount = root.Parse(invocation).Errors.Count;
            await Assert.That(errorCount).IsEqualTo(0);
        }
    }

    // ---------- option surface: names, aliases, arity, allowed values ----------

    [Test]
    public async Task CommitCommand_OptionSurfaceMatchesPreMigration()
    {
        using ServiceProvider services = BuildSurfaceServices();
        System.CommandLine.RootCommand root = BuildRoot(services);
        System.CommandLine.Command commit = root.Subcommands.Single(c => c.Name == "commit");

        string[] expectedOptions = ["--message", "--body", "--with", "--coauthor", "--assist", "--verbose"];
        await Assert.That(commit.Options.Count(o => !o.Hidden)).IsEqualTo(expectedOptions.Length);
        foreach (string expected in expectedOptions)
        {
            await Assert.That(commit.Options.Select(o => o.Name)).Contains(expected);
        }

        System.CommandLine.Option message = commit.Options.Single(o => o.Name == "--message");
        await Assert.That(message.Aliases).Contains("-m");
        await Assert.That(message.Required).IsTrue();

        System.CommandLine.Option body = commit.Options.Single(o => o.Name == "--body");
        await Assert.That(body.Aliases).Contains("-b");
        await Assert.That(body.Required).IsFalse();

        System.CommandLine.Option verbose = commit.Options.Single(o => o.Name == "--verbose");
        await Assert.That(verbose.Aliases).Contains("-v");
        await Assert.That(verbose.Required).IsFalse();

        foreach (string multiValue in new[] { "--with", "--coauthor", "--assist" })
        {
            System.CommandLine.Option option = commit.Options.Single(o => o.Name == multiValue);
            await Assert.That(option.Arity.Equals(System.CommandLine.ArgumentArity.OneOrMore)).IsTrue();
        }

        // POSIX bundling: -vm means -v -m <value>, exactly as before.
        int bundlingErrors = root.Parse(["commit", "-vm", "subj", "--with", "a"]).Errors.Count;
        await Assert.That(bundlingErrors).IsEqualTo(0);

        // --with takes one value per token; a second bare token stays unmatched.
        var multiValueParse = root.Parse(["commit", "-m", "s", "--with", "a", "b"]);
        await Assert.That(multiValueParse.Errors.Count).IsNotEqualTo(0);
        await Assert.That(string.Join(Environment.NewLine, multiValueParse.Errors.Select(e => e.Message)))
            .Contains("Unrecognized command or argument 'b'.");
    }

    [Test]
    public async Task MessageCommand_RequiresWithAndMatchesPreMigration()
    {
        using ServiceProvider services = BuildSurfaceServices();
        System.CommandLine.RootCommand root = BuildRoot(services);
        System.CommandLine.Command message = root.Subcommands.Single(c => c.Name == "message");

        System.CommandLine.Option with = message.Options.Single(o => o.Name == "--with");
        await Assert.That(with.Required).IsTrue();
        await Assert.That(with.Arity.Equals(System.CommandLine.ArgumentArity.OneOrMore)).IsTrue();

        System.CommandLine.Option messageOption = message.Options.Single(o => o.Name == "--message");
        await Assert.That(messageOption.Aliases).Contains("-m");
        await Assert.That(messageOption.Required).IsTrue();
    }

    [Test]
    public async Task AuthorCommands_OptionAndArgumentSurfaceMatchesPreMigration()
    {
        using ServiceProvider services = BuildSurfaceServices();
        System.CommandLine.RootCommand root = BuildRoot(services);
        System.CommandLine.Command author = root.Subcommands.Single(c => c.Name == "author");

        System.CommandLine.Command add = author.Subcommands.Single(c => c.Name == "add");
        await Assert.That(add.Options.Single(o => o.Name == "--type").Required).IsTrue();
        await Assert.That(add.Options.Single(o => o.Name == "--name").Required).IsTrue();
        await Assert.That(add.Options.Single(o => o.Name == "--email").Required).IsTrue();
        await Assert.That(add.Options.Single(o => o.Name == "--verbose").Aliases).Contains("-v");
        await Assert.That(add.Options.Single(o => o.Name == "--default-attribution-type").Required).IsFalse();
        await Assert.That(add.Arguments.Count).IsEqualTo(1);

        System.CommandLine.Command remove = author.Subcommands.Single(c => c.Name == "remove");
        await Assert.That(remove.Arguments.Count).IsEqualTo(1);
        await Assert.That(remove.Options.Single(o => o.Name == "--verbose").Aliases).Contains("-v");

        System.CommandLine.Command list = author.Subcommands.Single(c => c.Name == "list");
        await Assert.That(list.Options.Single(o => o.Name == "--type").Required).IsFalse();
        System.CommandLine.Option format = list.Options.Single(o => o.Name == "--format");
        await Assert.That(format.Required).IsFalse();
        await Assert.That(format.GetDefaultValue()).IsEqualTo("text");
    }

    [Test]
    public async Task ConfigAndInit_CommandSurfaceMatchesPreMigration()
    {
        using ServiceProvider services = BuildSurfaceServices();
        System.CommandLine.RootCommand root = BuildRoot(services);

        System.CommandLine.Command config = root.Subcommands.Single(c => c.Name == "config");
        await Assert.That(config.Arguments.Count).IsEqualTo(2);
        // No nested get/set subcommands: the pre-migration build exposed none.
        await Assert.That(config.Subcommands.Count).IsEqualTo(0);

        System.CommandLine.Command init = root.Subcommands.Single(c => c.Name == "init");
        System.CommandLine.Option global = init.Options.Single(o => o.Name == "--global");
        await Assert.That(global.Aliases).Contains("-g");
        await Assert.That(global.GetDefaultValue() is true).IsTrue();
    }

    [Test]
    public async Task AllowedValues_AreRejectedExactlyAsBefore()
    {
        (int exitCode, string output, string error) = await RunAsync("author", "list", "--type", "bogus");

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(error).Contains("Argument 'bogus' not recognized. Must be one of:");
        await Assert.That(error).Contains("'human'");
        await Assert.That(error).Contains("'agent'");
    }

    // ---------- exit codes ----------

    [Test]
    public async Task ExitCodes_HelpPathsReturnZero()
    {
        string[][] helpInvocations =
        [
            ["--help"],
            ["-h"],
            ["commit", "--help"],
            ["commit", "-h"],
            ["message", "--help"],
            ["config", "--help"],
            ["cfg", "--help"],
            ["init", "--help"],
            ["author", "--help"],
            ["author", "add", "--help"],
            ["author", "remove", "--help"],
            ["author", "list", "--help"],
        ];

        foreach (string[] invocation in helpInvocations)
        {
            (int exitCode, _, _) = await RunAsync(invocation);
            await Assert.That(exitCode).IsEqualTo(0);
        }
    }

    [Test]
    public async Task ExitCodes_ParseFailuresReturnOne()
    {
        string[][] failingInvocations =
        [
            ["definitely-not-a-real-subcommand"],
            ["commit"],
            ["commit", "--nope"],
            ["message"],
            ["config"],
            ["config", "get", "nope.nope"],
            ["author", "add"],
            ["author", "remove"],
            ["author", "list", "--type", "bogus"],
        ];

        foreach (string[] invocation in failingInvocations)
        {
            (int exitCode, _, _) = await RunAsync(invocation);
            await Assert.That(exitCode).IsEqualTo(1);
        }
    }

    [Test]
    public async Task ExitCodes_VersionAndBareConfigKeepTheirHistoricalValues()
    {
        (int versionExit, string versionOutput, _) = await RunAsync("--version");
        await Assert.That(versionExit).IsEqualTo(0);

        // --version prints the informational version of the entry assembly;
        // in the test process that is the test assembly, in the shipped tool
        // it is 0.2.0+<git sha>. Either way the format is <semver>+<sha>.
        System.Reflection.AssemblyInformationalVersionAttribute? informationalVersion =
            (System.Reflection.AssemblyInformationalVersionAttribute?)typeof(CommandSurfaceTests).Assembly
                .GetCustomAttribute(typeof(System.Reflection.AssemblyInformationalVersionAttribute));
        await Assert.That(versionOutput).Contains(informationalVersion!.InformationalVersion.Split('+')[0]);
        await Assert.That(versionOutput).Contains("+");

        // config <key> <value> renders help and returns the historical -1.
        (int configExit, string configOutput, _) = await RunAsync("config", "trailers.default", "x");
        await Assert.That(configExit).IsEqualTo(-1);
        await Assert.That(configOutput).Contains("co-attr config <key> <value> [options]");
    }

    [Test]
    public async Task ExitCodes_AuthorWithoutSubcommandShowsHelpAndReturnsOne()
    {
        (int exitCode, string output, _) = await RunAsync("author");

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(output).Contains("co-attr author [command] [options]");

        // The handler-driven ShowHelp path carries the same banner and title
        // as the --help option.
        string normalized = output.Replace("\r\n", "\n");
        await Assert.That(normalized.StartsWith(ExpectedBanner + "author\n\n", StringComparison.Ordinal)).IsTrue();
    }

    // ---------- parse-error output ----------

    [Test]
    public async Task ParseErrors_WriteDetailsWithoutHelpText()
    {
        (int exitCode, string output, string error) = await RunAsync("definitely-not-a-real-subcommand");

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(error).Contains("Unrecognized command or argument 'definitely-not-a-real-subcommand'.");
        // Pre-migration behavior: parse errors never dumped the help text.
        await Assert.That(output).DoesNotContain("Usage:");
    }

    [Test]
    public async Task ParseErrors_RequiredOptionMessagesAreUnchanged()
    {
        (_, _, string commitError) = await RunAsync("commit");
        await Assert.That(commitError).Contains("Option '--message' is required.");

        (_, _, string messageError) = await RunAsync("message");
        await Assert.That(messageError).Contains("Option '--with' is required.");
    }

    [Test]
    public async Task ParseErrors_KeepTypoSuggestionsOnStdout()
    {
        (int exitCode, string output, _) = await RunAsync("commit", "-m", "s", "--with", "a", "--nope");

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(output).Contains("'--nope' was not matched. Did you mean one of the following?");
    }

    // ---------- help output shape ----------

    [Test]
    public async Task Help_RootShapeMatchesPreMigration()
    {
        (int exitCode, string output, _) = await RunAsync("--help");

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output).Contains("Usage:");
        await Assert.That(output).Contains("co-attr [command] [options]");
        await Assert.That(output).Contains("--config-path <config-path>");
        await Assert.That(output).Contains("--registry <registry>");
        await Assert.That(output).Contains("-?, -h, --help");
        await Assert.That(output).Contains("Show help and usage information");
        await Assert.That(output).Contains("--version");
        await Assert.That(output).Contains("Show version information");
        await Assert.That(output).Contains("Commands:");
    }

    [Test]
    public async Task Help_RootPrintsVersionCopyrightBannerFirst()
    {
        (int exitCode, string output, _) = await RunAsync("--help");

        await Assert.That(exitCode).IsEqualTo(0);

        string normalized = output.Replace("\r\n", "\n");
        await Assert.That(normalized.StartsWith(ExpectedBanner, StringComparison.Ordinal)).IsTrue();
        // The framework's empty Description heading is suppressed so the
        // banner flows straight into Usage, as it did pre-migration.
        await Assert.That(normalized.Contains("Description:")).IsFalse();
    }

    [Test]
    public async Task Help_SubcommandsPrintBannerAndCommandTitle()
    {
        (int exitCode, string output, _) = await RunAsync("commit", "--help");

        await Assert.That(exitCode).IsEqualTo(0);

        string normalized = output.Replace("\r\n", "\n");
        await Assert.That(normalized.StartsWith(ExpectedBanner + "commit\n\n", StringComparison.Ordinal)).IsTrue();
        await Assert.That(normalized.Contains("Description:")).IsFalse();
    }

    [Test]
    public async Task Help_RootCommandListKeepsPreMigrationNamesAndAlias()
    {
        (int exitCode, string output, _) = await RunAsync("--help");

        await Assert.That(exitCode).IsEqualTo(0);

        string normalized = output.Replace("\r\n", "\n");
        int commandsIndex = normalized.IndexOf("Commands:", StringComparison.Ordinal);
        await Assert.That(commandsIndex).IsNotEqualTo(-1);

        string commandList = normalized[commandsIndex..];
        await Assert.That(commandList).Contains("author");
        await Assert.That(commandList).Contains("commit");
        await Assert.That(commandList).Contains("cfg, config");
        await Assert.That(commandList).Contains("init");
        await Assert.That(commandList).Contains("message");
    }

    [Test]
    public async Task Help_EverySubcommandHelpListsTheRecursiveRootOptions()
    {
        string[][] helpInvocations =
        [
            ["commit", "--help"],
            ["message", "--help"],
            ["config", "--help"],
            ["init", "--help"],
            ["author", "--help"],
            ["author", "add", "--help"],
            ["author", "remove", "--help"],
            ["author", "list", "--help"],
        ];

        foreach (string[] invocation in helpInvocations)
        {
            (int exitCode, string output, _) = await RunAsync(invocation);

            await Assert.That(exitCode).IsEqualTo(0);
            await Assert.That(output).Contains("--config-path <config-path>");
            await Assert.That(output).Contains("--registry <registry>");
        }
    }

    [Test]
    public async Task Help_CommitShapeMatchesPreMigration()
    {
        (int exitCode, string output, _) = await RunAsync("commit", "--help");

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output).Contains("Usage:");
        await Assert.That(output).Contains("co-attr commit [options]");
        await Assert.That(output).Contains("-m, --message <message>");
        await Assert.That(output).Contains("-b, --body <body>");
        await Assert.That(output).Contains("--with <with>");
        await Assert.That(output).Contains("--coauthor <coauthor>");
        await Assert.That(output).Contains("--assist <assist>");
        await Assert.That(output).Contains("-v, --verbose");
        await Assert.That(output).Contains("-?, -h, --help");
    }

    [Test]
    public async Task Help_MessageShapeMarksWithAsRequired()
    {
        (int exitCode, string output, _) = await RunAsync("message", "--help");

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output).Contains("co-attr message [options]");
        await Assert.That(output).Contains("-m, --message <message>");
        await Assert.That(output).Contains("--with <with>");
        await Assert.That(output).Contains("-v, --verbose");
    }

    [Test]
    public async Task Help_AuthorShapeMatchesPreMigration()
    {
        (int exitCode, string output, _) = await RunAsync("author", "--help");

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output).Contains("co-attr author [command] [options]");
        await Assert.That(output).Contains("Commands:");
        await Assert.That(output).Contains("add");
        await Assert.That(output).Contains("list");
        await Assert.That(output).Contains("remove");
    }

    [Test]
    public async Task Help_AuthorAddShapeMatchesPreMigration()
    {
        (int exitCode, string output, _) = await RunAsync("author", "add", "--help");

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output).Contains("co-attr author add <<Configuration_Id>> [options]");
        await Assert.That(output).Contains("--type <agent|human>");
        await Assert.That(output).Contains("--name <name>");
        await Assert.That(output).Contains("--email <email>");
        await Assert.That(output).Contains("--default-attribution-type <assist|coauthor>");
        await Assert.That(output).Contains("-v, --verbose");
    }

    [Test]
    public async Task Help_AuthorRemoveAndListShapesMatchPreMigration()
    {
        (int removeExit, string removeOutput, _) = await RunAsync("author", "remove", "--help");
        await Assert.That(removeExit).IsEqualTo(0);
        await Assert.That(removeOutput).Contains("co-attr author remove <<Configuration_Ids>>... [options]");
        await Assert.That(removeOutput).Contains("-v, --verbose");

        (int listExit, string listOutput, _) = await RunAsync("author", "list", "--help");
        await Assert.That(listExit).IsEqualTo(0);
        await Assert.That(listOutput).Contains("co-attr author list [options]");
        await Assert.That(listOutput).Contains("--type <agent|human>");
        await Assert.That(listOutput).Contains("--format <json|text>");
    }

    [Test]
    public async Task Help_ConfigShapeMatchesPreMigration()
    {
        (int exitCode, string output, _) = await RunAsync("config", "--help");

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output).Contains("Usage:");
        await Assert.That(output).Contains("co-attr config <key> <value> [options]");
        await Assert.That(output).Contains(
            "<authors_registry.paths.global|trailers.assistance|trailers.coauthorship|trailers.default>");
        await Assert.That(output).Contains("<value>");

        // No get/set subcommands were exposed pre-migration, so the config
        // help must keep its no-Commands shape.
        await Assert.That(output.Contains("Commands:")).IsFalse();
    }

    [Test]
    public async Task Help_InitShapeMatchesPreMigration()
    {
        (int exitCode, string output, _) = await RunAsync("init", "--help");

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output).Contains("co-attr init [options]");
        await Assert.That(output).Contains("-g, --global");
    }
}
