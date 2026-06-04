/*
    CoAuthorCli
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Text.Json;
using CoAuthor.Cli.Helpers.Contexts;

namespace CoAuthor.Cli.Commands;

[CliCommand(Name = "list", Parent = typeof(AuthorRootCommand))]
public class ListCoAuthorsCommand
{
    private readonly IGitCoAuthorInfoProvider _coAuthorInfoProvider;
    private readonly IConfiguration _configuration;

    public ListCoAuthorsCommand(IGitCoAuthorInfoProvider coAuthorInfoProvider, IConfiguration configuration)
    {
        _coAuthorInfoProvider = coAuthorInfoProvider;
        _configuration = configuration;
    }
    
    [CliOption(AllowedValues = ["human", "agent"], Name = "type", Required = false)]
    public string AuthorType { get; set; } = "";
    
    [CliOption(AllowedValues = ["json", "text"], Name = "format", Required = false)]
    public string Format { get; set; } = "text";
    
    public async Task<int> RunAsync(CliContext cliContext)
    {
        FileInfo configFile = FileHelper.ResolveConfigFile(_configuration);

        FileInfo authorsFile = await FileHelper.ResolveAuthorTomlFileAsync(configFile, cliContext.CancellationToken);

        GitCoAuthor[] storedCoAuthors = await _coAuthorInfoProvider.GetCoAuthorsAsync(authorsFile.FullName, cliContext.CancellationToken);

        GitCoAuthor[] coAuthorsToList;
        
        if (string.IsNullOrEmpty(AuthorType))
        {
            coAuthorsToList = storedCoAuthors;
        }
        else
        {
            coAuthorsToList = AuthorType.ToLower() switch
            {
                "agent" => storedCoAuthors.Where(c => c.Type == ContributorType.Agent).ToArray(),
                "human" => storedCoAuthors.Where(c => c.Type == ContributorType.Human).ToArray(),
                _ => storedCoAuthors
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
            Console.WriteLine(exception);
            throw;
        }
    }
}