/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.CommandLine;
using System.CommandLine.Invocation;
using CoAttribution.Cli.HostResolution;
using Microsoft.Extensions.DependencyInjection;

namespace CoAttribution.Cli.Commands;

public class CommitCommand
{
    private readonly ICommitOrchestrator _commitOrchestrator;

    public CommitCommand(ICommitOrchestrator commitOrchestrator)
    {
        _commitOrchestrator = commitOrchestrator;
    }
 
    public string SubjectMessage { get; set; } = "";
    
    public string BodyMessage { get; set; } = "";
    
    public string[] DefaultIds { get; set; } = [];
    
    public string[] CoAuthorIds  { get; set; } = [];
    
    public string[] AssistIds { get; set; } = [];
    
    public bool Verbose { get; set; } = false;

    /// <summary>Amend HEAD instead of creating a new commit.</summary>
    public bool Amend { get; set; } = false;

    /// <summary>Named git flag mapped to <c>git commit --no-verify</c>.</summary>
    public bool NoVerify { get; set; } = false;

    /// <summary>Named git flag mapped to <c>git commit --signoff</c>.</summary>
    public bool SignOff { get; set; } = false;

    /// <summary>
    /// Tokens captured after <c>--</c>, validated against the relay allowlist
    /// before anything is forwarded to git.
    /// </summary>
    public string[] RelayTokens { get; set; } = [];

    /// <summary>Builds the <c>commit</c> subcommand with its explicit options.</summary>
    public static Command Create(IServiceProvider services)
    {
        Option<string> messageOption = new("--message", "-m")
        {
            Arity = ArgumentArity.ExactlyOne,
            Required = true
        };
        Option<string> bodyOption = new("--body", "-b") { Arity = ArgumentArity.ExactlyOne };
        Option<string[]> withOption = new("--with") { Arity = ArgumentArity.OneOrMore };
        Option<string[]> coAuthorOption = new("--coauthor") { Arity = ArgumentArity.OneOrMore };
        Option<string[]> assistOption = new("--assist") { Arity = ArgumentArity.OneOrMore };
        Option<bool> verboseOption = new("--verbose", "-v");
        Option<bool> amendOption = new("--amend")
        {
            Description = "Amend the last commit instead of creating a new one."
        };
        Option<bool> noVerifyOption = new("--no-verify")
        {
            Description = "Skip git's pre-commit and commit-msg hooks (forwarded to git commit). " +
                          "Relayed flags are user-owned: CoAttribution relays flags and performs no staging itself."
        };
        Option<bool> signOffOption = new("--signoff")
        {
            Description = "Add a Signed-off-by line via git commit --signoff (forwarded to git commit). " +
                          "Relayed flags are user-owned: CoAttribution relays flags and performs no staging itself."
        };

        Command command = new("commit");
        command.Options.Add(messageOption);
        command.Options.Add(bodyOption);
        command.Options.Add(withOption);
        command.Options.Add(coAuthorOption);
        command.Options.Add(assistOption);
        command.Options.Add(verboseOption);
        command.Options.Add(amendOption);
        command.Options.Add(noVerifyOption);
        command.Options.Add(signOffOption);

        // Tokens after `--` are unmatched by design: the relay captures them
        // from the parse result instead of failing the parse. Stray tokens
        // typed before `--` are still reported as unrecognized below, so the
        // observable error surface stays as it was before the relay existed.
        command.TreatUnmatchedTokensAsErrors = false;

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            string[] relayTokens = GitFlagRelay.ExtractRelayedTokens(parseResult);
            string[] unrecognizedTokens = GitFlagRelay.ExtractUnrecognizedTokens(parseResult, relayTokens.Length);

            if (unrecognizedTokens.Length > 0)
            {
                foreach (string token in unrecognizedTokens)
                {
                    await Console.Error.WriteLineAsync(
                        string.Format(Resources.Commands_Commit_UnrecognizedArgument, token));
                }

                // Reproduce the framework's parse-failure output — typo
                // suggestions on stdout, error details on stderr — without
                // help text, exactly as an unmatched token printed before
                // the relay existed.
                _ = new ParseErrorAction { ShowHelp = false }.Invoke(parseResult);

                return 1;
            }

            CommitCommand handler = ActivatorUtilities.CreateInstance<CommitCommand>(services);
            handler.SubjectMessage = parseResult.GetValue(messageOption) ?? "";
            handler.BodyMessage = parseResult.GetValue(bodyOption) ?? "";
            handler.DefaultIds = parseResult.GetValue(withOption) ?? [];
            handler.CoAuthorIds = parseResult.GetValue(coAuthorOption) ?? [];
            handler.AssistIds = parseResult.GetValue(assistOption) ?? [];
            handler.Verbose = parseResult.GetValue(verboseOption);
            handler.Amend = parseResult.GetValue(amendOption);
            handler.NoVerify = parseResult.GetValue(noVerifyOption);
            handler.SignOff = parseResult.GetValue(signOffOption);
            handler.RelayTokens = relayTokens;

            return await handler.RunAsync(CliContext.From(parseResult, cancellationToken));
        });

        return command;
    }
 
    public async Task<int> RunAsync(CliContext cliContext)
    {
        try
        {
            // Relay validation runs first: a rejected token is named on
            // stderr and the command exits nonzero before any git
            // invocation (build, amend read, or commit) can happen.
            string[] namedGitFlags = BuildNamedGitFlags();
            string? relayRejection = GitFlagRelay.Validate(RelayTokens, namedGitFlags);

            if (relayRejection is not null)
            {
                await Console.Error.WriteLineAsync(relayRejection);
                return 1;
            }

            string[] forwardedArgs = [.. namedGitFlags, .. RelayTokens];

            CommitRequest commitRequest = new(SubjectMessage, BodyMessage,
                DefaultIds, CoAuthorIds, AssistIds, Amend)
            {
                RelayedArgs = forwardedArgs
            };

            GitResult result = Amend
                ? await _commitOrchestrator.AmendCommitAsync(commitRequest, cliContext.CancellationToken)
                : await _commitOrchestrator.ExecuteCommitAsync(
                    await _commitOrchestrator.BuildCommitMessageAsync(commitRequest, cliContext.CancellationToken),
                    cliContext.CancellationToken);

            // Surface git's own stdout and stderr verbatim: on success the user
            // sees git's commit summary; on failure they see git's actual error
            // (hooks, staging, identity problems) instead of a bare exit code.
            if (!string.IsNullOrWhiteSpace(result.StandardOutput))
                await Console.Out.WriteLineAsync(result.StandardOutput);
            if (!string.IsNullOrWhiteSpace(result.StandardError))
                await Console.Error.WriteLineAsync(result.StandardError);

            Console.WriteLine();
            Console.WriteLine();

            return result.ExitCode;
        }
        catch (AmendWithoutCommitException amendEx)
        {
            // Nothing to amend (no commits yet, or not a repository): show
            // the full amend-time diagnosis instead of the generic failure.
            await Console.Error.WriteLineAsync(amendEx.Message);

            return 1;
        }
        catch (MissingHostBlockException mhbEx)
        {
            MissingHostBlockDiagnosticFormatter formatter = new();

            foreach (MissingHostBlockDiagnostic diagnostic in mhbEx.Diagnostics)
            {
                await Console.Error.WriteLineAsync(formatter.Format(diagnostic));
                await Console.Error.WriteLineAsync();
            }

            return 1;
        }
        catch (UnknownCoAuthorException unknownEx)
        {
            // Unknown author IDs are a user error that must be loud: show
            // every unknown ID plus the registered IDs, regardless of --verbose.
            await Console.Error.WriteLineAsync(unknownEx.Message);

            return 1;
        }
        catch(Exception exception)
        {
            await Console.Error.WriteLineAsync(string.Format(Resources.Commands_Commit_Failed_Generic, SubjectMessage));
          
            if (Verbose)
            {
                await Console.Error.WriteLineAsync();
                
                await Console.Error.WriteLineAsync(Resources.Commands_Exceptions_Details + exception.Message);
            }
            
            return 1;
        }
    }

    /// <summary>
    /// The git flags the named options map to, spelled exactly as git knows
    /// them. They join the forwarded argument list ahead of the relayed
    /// tokens and count as "already given" for duplicate detection.
    /// </summary>
    private string[] BuildNamedGitFlags()
    {
        List<string> flags = [];

        if (NoVerify)
            flags.Add("--no-verify");

        if (SignOff)
            flags.Add("--signoff");

        return [.. flags];
    }
}