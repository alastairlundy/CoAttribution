/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.CommandLine;
using CoAttribution.Cli.HostResolution;
using Microsoft.Extensions.DependencyInjection;

namespace CoAttribution.Cli.Commands;

public class MessageCommand
{
    private readonly ICommitOrchestrator _commitOrchestrator;
    
    public MessageCommand(ICommitOrchestrator commitOrchestrator)
    {
        _commitOrchestrator = commitOrchestrator;
    }
    
    public string SubjectMessage { get; set; } = "";
    
    public string BodyMessage { get; set; } = "";
    
    public string[] DefaultIds { get; set; } = [];
    
    public string[] CoAuthorIds  { get; set; } = [];
    
    public string[] AssistIds { get; set; } = [];
    
    // ReSharper disable once RedundantDefaultMemberInitializer
    public bool Verbose { get; set; } = false;

    /// <summary>Preview the rebuilt amended message without committing.</summary>
    public bool Amend { get; set; } = false;

    /// <summary>Builds the <c>message</c> subcommand with its explicit options.</summary>
    public static Command Create(IServiceProvider services)
    {
        Option<string> messageOption = new("--message", "-m")
        {
            Arity = ArgumentArity.ExactlyOne
        };
        Option<string> bodyOption = new("--body", "-b") { Arity = ArgumentArity.ExactlyOne };
        Option<string[]> withOption = new("--with")
        {
            Arity = ArgumentArity.OneOrMore
        };
        Option<string[]> coAuthorOption = new("--coauthor") { Arity = ArgumentArity.OneOrMore };
        Option<string[]> assistOption = new("--assist") { Arity = ArgumentArity.OneOrMore };
        Option<bool> verboseOption = new("--verbose", "-v");
        Option<bool> amendOption = new("--amend")
        {
            Description = "Preview the amended message from the last commit without committing."
        };

        Command command = new("message");
        command.Options.Add(messageOption);
        command.Options.Add(bodyOption);
        command.Options.Add(withOption);
        command.Options.Add(coAuthorOption);
        command.Options.Add(assistOption);
        command.Options.Add(verboseOption);
        command.Options.Add(amendOption);

        // --message and --with are required for a fresh message but not for
        // --amend, which rebuilds from the last commit's message. The
        // framework's required flag is unconditional, so the requirement is
        // enforced conditionally here instead, in the framework's own
        // wording, keeping the historical parse-error text (and exit code)
        // for every non-amend invocation.
        command.Validators.Add(commandResult =>
        {
            if (commandResult.GetValue(amendOption))
                return;

            if (commandResult.GetResult(messageOption) is null)
                commandResult.AddError(string.Format(Resources.Commands_Message_OptionRequired, "--message"));

            if (commandResult.GetResult(withOption) is null)
                commandResult.AddError(string.Format(Resources.Commands_Message_OptionRequired, "--with"));
        });

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            MessageCommand handler = ActivatorUtilities.CreateInstance<MessageCommand>(services);
            handler.SubjectMessage = parseResult.GetValue(messageOption) ?? "";
            handler.BodyMessage = parseResult.GetValue(bodyOption) ?? "";
            handler.DefaultIds = parseResult.GetValue(withOption) ?? [];
            handler.CoAuthorIds = parseResult.GetValue(coAuthorOption) ?? [];
            handler.AssistIds = parseResult.GetValue(assistOption) ?? [];
            handler.Verbose = parseResult.GetValue(verboseOption);
            handler.Amend = parseResult.GetValue(amendOption);

            return await handler.RunAsync(CliContext.From(parseResult, cancellationToken));
        });

        return command;
    }

    /// <summary>
    /// Builds the requested message and prints it to stdout — or, with
    /// <see cref="Amend"/>, the rebuilt amended message the commit amend path
    /// would produce (the same last-commit read, the managed-token strip per
    /// the flags-present rule, and the current invocation's trailers).
    /// </summary>
    /// <remarks>
    /// This path only ever builds: <see cref="ICommitOrchestrator.BuildCommitMessageAsync"/>
    /// never executes a commit, so neither mode can reach <c>git commit</c>.
    /// </remarks>
    public async Task<int> RunAsync(CliContext cliContext)  
    {
        try
        {
            CommitMessage commitMessage = await _commitOrchestrator.BuildCommitMessageAsync(new CommitRequest(SubjectMessage, BodyMessage,
                    DefaultIds, CoAuthorIds, AssistIds, Amend),
                cliContext.CancellationToken);

            await Console.Out.WriteLineAsync();
            await Console.Out.WriteLineAsync();

            await Console.Out.WriteLineAsync(commitMessage.ToConsoleFormat());

            return 0;
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
            await Console.Error.WriteLineAsync(string.Format(Resources.Commands_Message_Failed, SubjectMessage));
          
            if (Verbose)
            {
                await Console.Error.WriteLineAsync();
                
                await Console.Error.WriteLineAsync(Resources.Commands_Exceptions_Details + exception.Message);
            }
            
            return 1;
        }
    }
}