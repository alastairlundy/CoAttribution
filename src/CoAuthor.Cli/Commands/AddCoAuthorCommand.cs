/*
    CoAuthorCli
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAuthor.Cli.Commands;

[CliCommand(Name = "add", Parent = typeof(AuthorRootCommand))]
public class AddCoAuthorCommand
{
    private readonly IGitCoAuthorInfoProvider _coAuthorInfoProvider;
    private readonly IConfiguration _configuration;

    public AddCoAuthorCommand(IGitCoAuthorInfoProvider coAuthorInfoProvider, IConfiguration configuration)
    {
        _coAuthorInfoProvider = coAuthorInfoProvider;
        _configuration = configuration;
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
        string configFile = ConfigurationFileHelper.ResolveConfigFile(_configuration);

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
            bool success = await _coAuthorInfoProvider.AddCoAuthorAsync(configFile, newCoAuthor, cliContext.CancellationToken);

            Console.WriteLine(Resources.Commands_Authors_Add_Successful, newCoAuthor, Path.GetFullPath(configFile));
        
            return success ? 0 : 1;
        }
        catch (Exception exception)
        {
            Console.WriteLine(Resources.Commands_Authors_Add_Failed, newCoAuthor, configFile);
            
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