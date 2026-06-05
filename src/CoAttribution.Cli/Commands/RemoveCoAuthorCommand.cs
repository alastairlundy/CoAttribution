/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Cli.Commands;

[CliCommand(Name = "remove", Parent = typeof(AuthorRootCommand))]
public class RemoveCoAuthorCommand
{
    private readonly IAuthorRegistry _authorRegistry;

    public RemoveCoAuthorCommand(IAuthorRegistry authorRegistry)
    {
        _authorRegistry = authorRegistry;
    }
    
    [CliArgument(Name = "<Configuration_Ids>", Order = 0, Required = true, Arity = CliArgumentArity.OneOrMore)]
    public string[] Ids { get; set; } = [];
    
    [CliOption(Name = "verbose", Alias =  "v", Required = false)]
    // ReSharper disable once RedundantDefaultMemberInitializer
    public bool Verbose { get; set; } = false;


    public async Task<int> RunAsync(CliContext cliContext)
    {
        try
        {
            await _authorRegistry.RemoveAsync(Ids, cliContext.CancellationToken);

            return 0;
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