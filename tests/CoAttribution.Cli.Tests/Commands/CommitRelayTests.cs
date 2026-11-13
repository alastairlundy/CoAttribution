using CoAttribution.Cli.Tests.Fakes;
using CoAttribution.Cli.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute.ExceptionExtensions;

namespace CoAttribution.Cli.Tests.Commands;

/// <summary>
/// Covers the user-owned git flag relay on <c>commit</c>: allowlist matching
/// including attached values, every rejection class with an error that names
/// the offending token, duplicate rejection, the named-flag mapping into the
/// forwarded argument list, the amend invocation path, and the parse-level
/// capture of tokens after <c>--</c>.
/// </summary>
[NotInParallel]
public class CommitRelayTests
{
    private static ICommitOrchestrator SuccessfulOrchestrator()
    {
        ICommitOrchestrator orchestrator = Substitute.For<ICommitOrchestrator>();
        orchestrator.BuildCommitMessageAsync(Arg.Any<CommitRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CommitMessage("feat: x", [], []));
        orchestrator.ExecuteCommitAsync(Arg.Any<CommitMessage>(), Arg.Any<CancellationToken>())
            .Returns(new GitResult(0, "ok", ""));
        orchestrator.AmendCommitAsync(Arg.Any<CommitRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GitResult(0, "ok", ""));
        return orchestrator;
    }

    private static CommitCommand BuildCommand(ICommitOrchestrator orchestrator)
    {
        CommitCommand command = CommandTestHarness.BuildCommitCommand(orchestrator);
        command.SubjectMessage = "feat: x";
        return command;
    }

    private static async Task AssertNoGitInvoked(ICommitOrchestrator orchestrator)
    {
        await orchestrator.DidNotReceive().BuildCommitMessageAsync(Arg.Any<CommitRequest>(), Arg.Any<CancellationToken>());
        await orchestrator.DidNotReceive().ExecuteCommitAsync(Arg.Any<CommitMessage>(), Arg.Any<CancellationToken>());
        await orchestrator.DidNotReceive().AmendCommitAsync(Arg.Any<CommitRequest>(), Arg.Any<CancellationToken>());
    }

    // ---------- capture from the parse result ----------

    [Test]
    public async Task ExtractRelayedTokens_ReturnsTokensAfterTheDoubleDashVerbatim()
    {
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        System.CommandLine.RootCommand root = RootCommand.Create(services);

        System.CommandLine.ParseResult parseResult = root.Parse(
            ["commit", "-m", "s", "--", "--allow-empty", "--author=Relay Bot <relay@example.com>"]);

        string[] relayed = GitFlagRelay.ExtractRelayedTokens(parseResult);

        await Assert.That(string.Join("|", relayed))
            .IsEqualTo("--allow-empty|--author=Relay Bot <relay@example.com>");
    }

    [Test]
    public async Task ExtractRelayedTokens_WithoutDoubleDash_ReturnsEmpty()
    {
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        System.CommandLine.RootCommand root = RootCommand.Create(services);

        System.CommandLine.ParseResult parseResult = root.Parse(["commit", "-m", "s", "--signoff"]);

        await Assert.That(GitFlagRelay.ExtractRelayedTokens(parseResult)).IsEmpty();
    }

    [Test]
    public async Task ExtractUnrecognizedTokens_SplitsStraysTypedBeforeTheSeparator()
    {
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        System.CommandLine.RootCommand root = RootCommand.Create(services);

        System.CommandLine.ParseResult parseResult = root.Parse(["commit", "-m", "s", "--nope", "--", "-a"]);
        string[] relayed = GitFlagRelay.ExtractRelayedTokens(parseResult);
        string[] unrecognized = GitFlagRelay.ExtractUnrecognizedTokens(parseResult, relayed.Length);

        await Assert.That(string.Join("|", relayed)).IsEqualTo("-a");
        await Assert.That(string.Join("|", unrecognized)).IsEqualTo("--nope");
    }

    // ---------- allowlist acceptance ----------

    [Test]
    public async Task Validate_AllSixAllowlistedFlags_AreAccepted()
    {
        string[] allowlist = ["-a", "--no-verify", "--signoff", "--allow-empty", "--author", "--no-gpg-sign"];

        foreach (string flag in allowlist)
        {
            string? rejection = GitFlagRelay.Validate([flag], []);

            await Assert.That(rejection).IsNull();
        }
    }

    [Test]
    public async Task Validate_AuthorValueAttachedOrSeparate_PassesWithItsFlag()
    {
        await Assert.That(GitFlagRelay.Validate(["--author=Relay Bot <relay@example.com>"], [])).IsNull();
        await Assert.That(GitFlagRelay.Validate(["--author", "Relay Bot <relay@example.com>"], [])).IsNull();
    }

    // ---------- rejection classes, each naming the token ----------

    [Test]
    [Arguments("-m")]
    [Arguments("-F")]
    [Arguments("--message")]
    [Arguments("--trailer")]
    [Arguments("--amend")]
    public async Task Validate_ToolManagedTokens_AreRejectedByName(string token)
    {
        string rejection = GitFlagRelay.Validate([token], []) ?? "";

        await Assert.That(rejection).Contains(token);
        await Assert.That(rejection).Contains("https://github.com/alastairlundy/CoAttribution/issues");
    }

    [Test]
    [Arguments("--config-path")]
    [Arguments("--registry")]
    public async Task Validate_BootstrapOwnedTokens_AreRejectedByName(string token)
    {
        string rejection = GitFlagRelay.Validate([token], []) ?? "";

        await Assert.That(rejection).Contains(token);
        await Assert.That(rejection).Contains("https://github.com/alastairlundy/CoAttribution/issues");
    }

    [Test]
    public async Task Validate_UnknownToken_IsRejectedAsNotAllowlisted()
    {
        string rejection = GitFlagRelay.Validate(["--bogus"], []) ?? "";

        await Assert.That(rejection).Contains("--bogus");
        await Assert.That(rejection).Contains("not on the allowlist");
    }

    [Test]
    public async Task Validate_BareTokenWithoutAFlagToRideWith_IsRejected()
    {
        // A stray pathspec-like token must not slip through unvalidated.
        string rejection = GitFlagRelay.Validate(["somefile.txt"], []) ?? "";

        await Assert.That(rejection).Contains("somefile.txt");
    }

    [Test]
    public async Task Validate_DuplicateInsideTheRelay_IsRejected()
    {
        string rejection = GitFlagRelay.Validate(["--allow-empty", "--allow-empty"], []) ?? "";

        await Assert.That(rejection).Contains("--allow-empty");
        await Assert.That(rejection).Contains("already given");
    }

    [Test]
    public async Task Validate_RelayedDuplicateOfANamedFlag_IsRejected()
    {
        string rejection = GitFlagRelay.Validate(["--signoff"], ["--signoff"]) ?? "";

        await Assert.That(rejection).Contains("--signoff");
        await Assert.That(rejection).Contains("already given");
    }

    // ---------- forwarding ----------

    [Test]
    public async Task RunAsync_AllowlistedRelay_ForwardsTokensVerbatimAfterTheNamedFlags()
    {
        ICommitOrchestrator orchestrator = SuccessfulOrchestrator();
        CommitCommand command = BuildCommand(orchestrator);
        command.NoVerify = true;
        command.RelayTokens = ["--allow-empty", "--author=Relay Bot <relay@example.com>"];

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();
        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(0);
        await orchestrator.Received(1).BuildCommitMessageAsync(
            Arg.Is<CommitRequest>(r => r.RelayedArgs.SequenceEqual(
                new[] { "--no-verify", "--allow-empty", "--author=Relay Bot <relay@example.com>" })),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RunAsync_NamedGitFlags_MapToTheirGitSpellings()
    {
        ICommitOrchestrator orchestrator = SuccessfulOrchestrator();
        CommitCommand command = BuildCommand(orchestrator);
        command.NoVerify = true;
        command.SignOff = true;
        command.RelayTokens = ["-a"];

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();
        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(0);
        await orchestrator.Received(1).BuildCommitMessageAsync(
            Arg.Is<CommitRequest>(r => r.RelayedArgs.SequenceEqual(new[] { "--no-verify", "--signoff", "-a" })),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments("--bogus")]
    [Arguments("-m")]
    [Arguments("--config-path")]
    [Arguments("--registry")]
    public async Task RunAsync_RejectedRelay_NamesTheTokenOnStderrAndSkipsGit(string token)
    {
        ICommitOrchestrator orchestrator = SuccessfulOrchestrator();
        CommitCommand command = BuildCommand(orchestrator);
        command.RelayTokens = [token];

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();
        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(console.StandardError).Contains(token);
        await Assert.That(console.StandardError).Contains("https://github.com/alastairlundy/CoAttribution/issues");
        await AssertNoGitInvoked(orchestrator);
    }

    // ---------- amend invocation path ----------

    [Test]
    public async Task RunAsync_Amend_TriggersTheLibraryAmendPathWithRelayedFlags()
    {
        ICommitOrchestrator orchestrator = SuccessfulOrchestrator();
        CommitCommand command = BuildCommand(orchestrator);
        command.Amend = true;
        command.RelayTokens = ["--signoff"];

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();
        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(0);
        await orchestrator.Received(1).AmendCommitAsync(
            Arg.Is<CommitRequest>(r => r.Amend && r.RelayedArgs.SequenceEqual(new[] { "--signoff" })),
            Arg.Any<CancellationToken>());
        await orchestrator.DidNotReceive().ExecuteCommitAsync(Arg.Any<CommitMessage>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RunAsync_AmendWithoutCommit_SurfacesTheDiagnosisAndReturnsOne()
    {
        ICommitOrchestrator orchestrator = Substitute.For<ICommitOrchestrator>();
        orchestrator.AmendCommitAsync(Arg.Any<CommitRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AmendWithoutCommitException("fatal: not a git repository"));

        CommitCommand command = BuildCommand(orchestrator);
        command.Amend = true;

        using ConsoleCapture console = new();
        CliContext ctx = CliContextFactory.Create();
        int exitCode = await command.RunAsync(ctx);

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(console.StandardError).Contains("not a git repository");
        await Assert.That(console.StandardError).DoesNotContain("{0}");
    }

    // ---------- parse-level pipeline ----------

    [Test]
    public async Task CliRunner_StrayTokenBeforeTheDoubleDash_FailsWithNamedErrorAndTypoSuggestion()
    {
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        System.CommandLine.RootCommand root = RootCommand.Create(services);

        using ConsoleCapture console = new();
        int exitCode = await CliRunner.RunAsync(root, ["commit", "-m", "s", "--nope"]);

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(console.StandardError).Contains("Unrecognized command or argument '--nope'.");
        await Assert.That(console.StandardOutput).Contains("'--nope' was not matched. Did you mean one of the following?");
    }

    [Test]
    public async Task CliRunner_RejectedRelay_FailsTheCommandBeforeAnyGitInvocation()
    {
        ICommitOrchestrator orchestrator = SuccessfulOrchestrator();
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(CommandTestHarness.SingleValueConfiguration("config-file", ""));
        services.AddSingleton(Substitute.For<IConfigResolver>());
        services.AddSingleton(orchestrator);

        using ServiceProvider provider = services.BuildServiceProvider();
        System.CommandLine.RootCommand root = RootCommand.Create(provider);

        using ConsoleCapture console = new();
        int exitCode = await CliRunner.RunAsync(root, ["commit", "-m", "s", "--", "-m", "smuggled"]);

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(console.StandardError).Contains("'-m'");
        await AssertNoGitInvoked(orchestrator);
    }

    [Test]
    public async Task Help_CarriesTheUserOwnedRelayWording()
    {
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        System.CommandLine.RootCommand root = RootCommand.Create(services);

        using ConsoleCapture console = new();
        int exitCode = await CliRunner.RunAsync(root, ["commit", "--help"]);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(console.StandardOutput)
            .Contains("CoAttribution relays flags and performs no staging itself");
    }
}
