/*
    CoAuthorCli
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAuthor.Cli.Commands;

[CliCommand(Name = "remove", Parent = typeof(AuthorRootCommand))]
public class RemoveCoAuthorCommand
{
    private readonly IGitCoAuthorInfoProvider _coAuthorInfoProvider;
    private readonly IConfiguration _configuration;

    public RemoveCoAuthorCommand(IGitCoAuthorInfoProvider coAuthorInfoProvider, 
        IConfiguration configuration)
    {
        _coAuthorInfoProvider = coAuthorInfoProvider;
        _configuration = configuration;
    }
    
    [CliArgument(Name = "<Configuration_Ids>", Order = 0, Required = true, Arity = CliArgumentArity.OneOrMore)]
    public string[] Ids { get; set; } = [];
    
    [CliOption(Name = "verbose", Alias =  "v", Required = false)]
    // ReSharper disable once RedundantDefaultMemberInitializer
    public bool Verbose { get; set; } = false;


    public async Task<int> RunAsync(CliContext cliContext)
    {
        FileInfo configFile = FileHelper.ResolveConfigFile(_configuration);

        FileInfo authorsFile = await FileHelper.ResolveAuthorTomlFileAsync(configFile, cliContext.CancellationToken);

        try
        {
            ArgumentException.ThrowIfNullOrEmpty(configFile.FullName);
            
            GitCoAuthor[] storedCoAuthors =
                await _coAuthorInfoProvider.GetCoAuthorsAsync(authorsFile.FullName, cliContext.CancellationToken);

            GitCoAuthor[] actualCoAuthors = storedCoAuthors.Join(
                    Ids,
                    outerKey => outerKey.CoAuthorId,
                    innerKey => innerKey,
                    (outerItem, _) => (outerItem))
                .DistinctBy(i => i.CoAuthorId)
                .ToArray();

            bool success =
                await _coAuthorInfoProvider.RemoveCoAuthorsAsync(authorsFile.FullName, actualCoAuthors, cliContext.CancellationToken);

            return success ? 0 : 1;
        }
        catch (Exception exception)
        {
            Console.WriteLine(Resources.Commands_Authors_Remove_Failed, string.Join(", ", Ids), authorsFile.FullName);
            
            if (Verbose)
            {
                Console.WriteLine();
                throw;
            }
            
            Console.WriteLine(Resources.Commands_Exceptions_Details + exception.Message);

            return 1;
        }
    }
}