/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace CoAttribution.Cli.Commands;

public class RemoveCoAuthorCommand
{
    private readonly IAuthorRegistry _authorRegistry;

    public RemoveCoAuthorCommand(IAuthorRegistry authorRegistry)
    {
        _authorRegistry = authorRegistry;
    }
    
    public string[] Ids { get; set; } = [];
    
    // ReSharper disable once RedundantDefaultMemberInitializer
    public bool Verbose { get; set; } = false;

    /// <summary>Builds the <c>author remove</c> subcommand with its explicit arguments and options.</summary>
    public static Command Create(IServiceProvider services)
    {
        Argument<string[]> idsArgument = new("<Configuration_Ids>") { Arity = ArgumentArity.OneOrMore };
        Option<bool> verboseOption = new("--verbose", "-v");

        Command command = new("remove");
        command.Arguments.Add(idsArgument);
        command.Options.Add(verboseOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            RemoveCoAuthorCommand handler = ActivatorUtilities.CreateInstance<RemoveCoAuthorCommand>(services);
            handler.Ids = parseResult.GetValue(idsArgument) ?? [];
            handler.Verbose = parseResult.GetValue(verboseOption);

            return await handler.RunAsync(CliContext.From(parseResult, cancellationToken));
        });

        return command;
    }

    public async Task<int> RunAsync(CliContext cliContext)
    {
        FileInfo? authorsFile = await _authorRegistry.GetRegistryFileAsync(cliContext.CancellationToken);

        try
        {
            await _authorRegistry.RemoveAsync(Ids, cliContext.CancellationToken);

            await Console.Out.WriteLineAsync(string.Format(Resources.Commands_Authors_Remove_Successful,
                string.Join(", ", Ids), authorsFile?.FullName ?? "N/A"));

            return 0;
        }
        catch (Exception exception)
        {
            string error = string.Format(Resources.Commands_Authors_Remove_Failed, string.Join(", ", Ids), authorsFile?.FullName ?? "N/A");
            
            await Console.Error.WriteLineAsync(error);
            
            if (Verbose)
            { 
                await Console.Error.WriteLineAsync();
                
                await Console.Error.WriteLineAsync(Resources.Commands_Exceptions_Details + exception.Message);
            }

            return 1;
        }
    }
}