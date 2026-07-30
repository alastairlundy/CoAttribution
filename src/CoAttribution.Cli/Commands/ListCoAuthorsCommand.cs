/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Text.Json;

namespace CoAttribution.Cli.Commands;

[CliCommand(Name = "list", Parent = typeof(AuthorRootCommand))]
public class ListCoAuthorsCommand
{
    private readonly IAuthorRegistry _authorRegistry;

    public ListCoAuthorsCommand(IAuthorRegistry authorRegistry)
    {
        _authorRegistry = authorRegistry;
    }
    
    [CliOption(AllowedValues = ["human", "agent"], Name = "type", Required = false)]
    public string AuthorType { get; set; } = "";
    
    [CliOption(AllowedValues = ["json", "text"], Name = "format", Required = false)]
    public string Format { get; set; } = "text";
    
    public async Task<int> RunAsync(CliContext cliContext)
    {
        GitCoAuthorConfig config = await _authorRegistry.GetAuthorConfigAsync(cliContext.CancellationToken);

        GitCoAuthor[] coAuthorsToList;
        
        if (string.IsNullOrEmpty(AuthorType))
        {
            coAuthorsToList = config.GetCoAuthors();
        }
        else
        {
            coAuthorsToList = AuthorType.ToLower() switch
            {
                "agent" => config.Agents.Select(a => a.Value).ToArray(),
                "human" => config.Humans.Select(h  => h.Value).ToArray(),
                _ => config.GetCoAuthors()
            };
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