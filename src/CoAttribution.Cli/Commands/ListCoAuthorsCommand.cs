/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace CoAttribution.Cli.Commands;

public class ListCoAuthorsCommand
{
    private readonly IAuthorRegistry _authorRegistry;

    public ListCoAuthorsCommand(IAuthorRegistry authorRegistry)
    {
        _authorRegistry = authorRegistry;
    }
    
    public string AuthorType { get; set; } = "";
    
    public string Format { get; set; } = "text";

    /// <summary>Builds the <c>author list</c> subcommand with its explicit options.</summary>
    public static Command Create(IServiceProvider services)
    {
        Option<string> typeOption = new("--type");
        typeOption.AcceptOnlyFromAmong("human", "agent");

        Option<string> formatOption = new("--format")
        {
            DefaultValueFactory = _ => "text"
        };
        formatOption.AcceptOnlyFromAmong("json", "text");

        Command command = new("list");
        command.Options.Add(typeOption);
        command.Options.Add(formatOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            ListCoAuthorsCommand handler = ActivatorUtilities.CreateInstance<ListCoAuthorsCommand>(services);
            handler.AuthorType = parseResult.GetValue(typeOption) ?? "";
            handler.Format = parseResult.GetValue(formatOption) ?? "text";

            return await handler.RunAsync(CliContext.From(parseResult, cancellationToken));
        });

        return command;
    }

    public async Task<int> RunAsync(CliContext cliContext)
    {
        GitCoAuthorConfig config = await _authorRegistry.GetAuthorConfigAsync(cliContext.CancellationToken);

        GitCoAuthor[] coAuthorsToList;
        
        if (string.IsNullOrEmpty(AuthorType))
        {
            coAuthorsToList = config.GetCoAuthors();
        }
        else if (AuthorType.Equals("agent", StringComparison.OrdinalIgnoreCase))
        {
            coAuthorsToList = config.Agents.Select(a => a.Value).ToArray();
        }
        else if (AuthorType.Equals("human", StringComparison.OrdinalIgnoreCase))
        {
            coAuthorsToList = config.Humans.Select(h => h.Value).ToArray();
        }
        else
        {
            coAuthorsToList = config.GetCoAuthors();
        }

        try
        {
            if (Format.Equals("json", StringComparison.CurrentCultureIgnoreCase))
            {
                string jsonText = JsonSerializer.Serialize<GitCoAuthor[]>(coAuthorsToList, CoAuthorJsonContext.Default.GitCoAuthorArray);
                
                await Console.Out.WriteLineAsync(jsonText);
            }
            else
            {
                foreach (GitCoAuthor coAuthor in coAuthorsToList)
                {
                    await Console.Out.WriteLineAsync(coAuthor.ToString());
                }
            }

            return 0;
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync(exception.Message);

            return 1;
        }
    }
}