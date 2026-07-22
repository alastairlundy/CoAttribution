/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Cli.Commands;

[CliCommand(Name = "add", Parent = typeof(AuthorRootCommand))]
public class AddCoAuthorCommand
{
    private readonly IAuthorRegistry _authorRegistry;

    public AddCoAuthorCommand(IAuthorRegistry authorRegistry)
    {
        _authorRegistry = authorRegistry;
    }
    
    [CliArgument(Name = "<Configuration_Id>", Order = 0, Required = true, Arity =  CliArgumentArity.ExactlyOne)]
    public string Id { get; set; } = "";

    [CliOption(AllowedValues = ["human", "agent"], Name = "type", Required = true)]
    public string AuthorType { get; set; } = "";
    
    [CliOption(Name = "name", Required = true)]
    public string AuthorName { get; set; } = "";
    
    [CliOption(Name = "email", Required = true)]
    public string AuthorEmail { get; set; } = "";

    [CliOption(Name = "default-attribution-type", Required = false, AllowedValues = ["assist", "coauthor"])]
    public string DefaultAttributionType { get; set; } = "";
    
    [CliOption(Name = "verbose", Alias =  "v", Required = false)]
    // ReSharper disable once RedundantDefaultMemberInitializer
    public bool Verbose { get; set; } = false;
    
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