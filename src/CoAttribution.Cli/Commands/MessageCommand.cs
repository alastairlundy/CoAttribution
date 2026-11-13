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

    /// <summary>Builds the <c>message</c> subcommand with its explicit options.</summary>
    public static Command Create(IServiceProvider services)
    {
        Option<string> messageOption = new("--message", "-m")
        {
            Arity = ArgumentArity.ExactlyOne,
            Required = true
        };
        Option<string> bodyOption = new("--body", "-b") { Arity = ArgumentArity.ExactlyOne };
        Option<string[]> withOption = new("--with")
        {
            Arity = ArgumentArity.OneOrMore,
            Required = true
        };
        Option<string[]> coAuthorOption = new("--coauthor") { Arity = ArgumentArity.OneOrMore };
        Option<string[]> assistOption = new("--assist") { Arity = ArgumentArity.OneOrMore };
        Option<bool> verboseOption = new("--verbose", "-v");

        Command command = new("message");
        command.Options.Add(messageOption);
        command.Options.Add(bodyOption);
        command.Options.Add(withOption);
        command.Options.Add(coAuthorOption);
        command.Options.Add(assistOption);
        command.Options.Add(verboseOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            MessageCommand handler = ActivatorUtilities.CreateInstance<MessageCommand>(services);
            handler.SubjectMessage = parseResult.GetValue(messageOption) ?? "";
            handler.BodyMessage = parseResult.GetValue(bodyOption) ?? "";
            handler.DefaultIds = parseResult.GetValue(withOption) ?? [];
            handler.CoAuthorIds = parseResult.GetValue(coAuthorOption) ?? [];
            handler.AssistIds = parseResult.GetValue(assistOption) ?? [];
            handler.Verbose = parseResult.GetValue(verboseOption);

            return await handler.RunAsync(CliContext.From(parseResult, cancellationToken));
        });

        return command;
    }
    
    public async Task<int> RunAsync(CliContext cliContext)  
    {
        try
        {
            CommitMessage commitMessage = await _commitOrchestrator.BuildCommitMessageAsync(new CommitRequest(SubjectMessage, BodyMessage,
                    DefaultIds, CoAuthorIds, AssistIds),
                cliContext.CancellationToken);

            await Console.Out.WriteLineAsync();
            await Console.Out.WriteLineAsync();

            await Console.Out.WriteLineAsync(commitMessage.ToString());

            return 0;
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