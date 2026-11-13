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

public class AddCoAuthorCommand
{
    private readonly IAuthorRegistry _authorRegistry;

    public AddCoAuthorCommand(IAuthorRegistry authorRegistry)
    {
        _authorRegistry = authorRegistry;
    }
    
    public string Id { get; set; } = "";

    public string AuthorType { get; set; } = "";
    
    public string AuthorName { get; set; } = "";
    
    public string AuthorEmail { get; set; } = "";

    public string DefaultAttributionType { get; set; } = "";
    
    // ReSharper disable once RedundantDefaultMemberInitializer
    public bool Verbose { get; set; } = false;

    /// <summary>Builds the <c>author add</c> subcommand with its explicit arguments and options.</summary>
    public static Command Create(IServiceProvider services)
    {
        Argument<string> idArgument = new("<Configuration_Id>");

        Option<string> typeOption = new("--type") { Required = true };
        typeOption.AcceptOnlyFromAmong("human", "agent");

        Option<string> nameOption = new("--name") { Required = true };
        Option<string> emailOption = new("--email") { Required = true };

        Option<string> defaultAttributionTypeOption = new("--default-attribution-type");
        defaultAttributionTypeOption.AcceptOnlyFromAmong("assist", "coauthor");

        Option<bool> verboseOption = new("--verbose", "-v");

        Command command = new("add");
        command.Arguments.Add(idArgument);
        command.Options.Add(typeOption);
        command.Options.Add(nameOption);
        command.Options.Add(emailOption);
        command.Options.Add(defaultAttributionTypeOption);
        command.Options.Add(verboseOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            AddCoAuthorCommand handler = ActivatorUtilities.CreateInstance<AddCoAuthorCommand>(services);
            handler.Id = parseResult.GetValue(idArgument) ?? "";
            handler.AuthorType = parseResult.GetValue(typeOption) ?? "";
            handler.AuthorName = parseResult.GetValue(nameOption) ?? "";
            handler.AuthorEmail = parseResult.GetValue(emailOption) ?? "";
            handler.DefaultAttributionType = parseResult.GetValue(defaultAttributionTypeOption) ?? "";
            handler.Verbose = parseResult.GetValue(verboseOption);

            return await handler.RunAsync(CliContext.From(parseResult, cancellationToken));
        });

        return command;
    }
    
    public async Task<int> RunAsync(CliContext cliContext)
    {
        FileInfo? authorsFile = await _authorRegistry.GetRegistryFileAsync(cliContext.CancellationToken);
        
        GitCoAuthor newCoAuthor = new()
        {
            CoAuthorId = Id,
            Name = AuthorName,
            Email = AuthorEmail,
            Type = AuthorType.Equals("agent", StringComparison.CurrentCultureIgnoreCase) ?
                ContributorType.Agent : ContributorType.Human,
        };

        if (!string.IsNullOrEmpty(DefaultAttributionType))
            newCoAuthor.DefaultAttributionType =
                DefaultAttributionType.Equals("coauthor", StringComparison.CurrentCultureIgnoreCase)
                    ? AttributionType.CoAuthor
                    : AttributionType.Assisted;

        try
        {
            await _authorRegistry.AddAsync(newCoAuthor, cliContext.CancellationToken);

            Console.Out.WriteLine(Resources.Commands_Authors_Add_Successful, newCoAuthor, authorsFile?.FullName ?? "N/A");

            return 0;
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync(string.Format(Resources.Commands_Authors_Add_Failed, newCoAuthor, authorsFile?.FullName ?? "N/A"));
            
            if (Verbose)
            {
                await Console.Error.WriteLineAsync();
                
                await Console.Error.WriteLineAsync(Resources.Commands_Exceptions_Details + exception.Message);
            }
            
            return 1;
        }
    }
}