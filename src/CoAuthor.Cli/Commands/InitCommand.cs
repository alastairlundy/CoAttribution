/*
    CoAuthorCli
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAuthor.Cli.Components.Dialogs;
using DotExtensions.IO.Directories;
using Terminal.Gui.App;

namespace CoAuthor.Cli.Commands;

[CliCommand(Name = "init", Parent = typeof(RootCommand))]
public class InitCommand
{
    private readonly IConfiguration _configuration;

    public InitCommand(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    [CliOption(Name = "interactive", Alias = "i", Arity = CliArgumentArity.ZeroOrOne,
        Required = false)]
    // ReSharper disable once RedundantDefaultMemberInitializer
    public bool Interactive { get; set; } = false;
    
    [CliOption(Name = "config-path", Required = false,
        Arity = CliArgumentArity.ExactlyOne)]
    public string ConfigFilePath { get; set; } = string.Empty;
    
    public async Task<int> RunAsync()
    {
        ConfigFilePath = ConfigurationFileHelper.ResolveConfigFile(_configuration);


        try
        {
            string defaultTomlContents = "";
                
            FileInfo file = new(ConfigFilePath);
            DirectoryInfo directory = file.GetDirectory();
                
            Directory.CreateDirectory(directory.FullName);
                
            await File.WriteAllTextAsync(ConfigFilePath, defaultTomlContents);
                
            // TODO: Inform user of status of File Write.
            await Console.Out.WriteLineAsync("");


            return 0;
        }
        catch(Exception exception)
        {
            await Console.Error.WriteLineAsync("Couldn't initialise the config file");

            await Console.Error.WriteLineAsync($"Exception Details: {exception.Message}");
                
            return 1;
        }
    }
}