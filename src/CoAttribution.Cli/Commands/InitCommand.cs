/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using DotExtensions.IO.Directories;

/*using Terminal.Gui.App;
 using CoAttribution.Cli.Components.Dialogs;
*/

namespace CoAttribution.Cli.Commands;

[CliCommand(Name = "init", Parent = typeof(RootCommand))]
public class InitCommand
{
    private readonly IConfiguration _configuration;

    public InitCommand(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    /*[CliOption(Name = "interactive", Alias = "i", Arity = CliArgumentArity.ZeroOrOne,
        Required = false)]
    // ReSharper disable once RedundantDefaultMemberInitializer
    public bool Interactive { get; set; } = false;*/
    
    [CliOption(Name = "config-path", Required = false, Arity = CliArgumentArity.ExactlyOne)]
    public string ConfigFilePath { get; set; } = string.Empty;
    
    [CliOption(Name = "global", Alias = "g", Required = false, Arity = CliArgumentArity.ZeroOrOne)]
    public bool CreateGlobalFile { get; set; } = true;
    
    public async Task<int> RunAsync(CliContext cliContext)
    {
        if (string.IsNullOrEmpty(ConfigFilePath))
        {
            ConfigFilePath = _configuration["config-file"] ?? _configuration["coauthor_config_file"] ?? "";
        }

        try
        {
            if (!string.IsNullOrEmpty(ConfigFilePath) && !File.Exists(ConfigFilePath))
            {
                await CreateConfigFileAsync(cliContext.CancellationToken);
            }
            
            await Console.Out.WriteLineAsync(string.Format(Resources.Commands_Init_ConfigFileCreated, ConfigFilePath));
            
            await CreateAuthorsTomlFileAsync(cliContext.CancellationToken);
            
            return 0;
        }
        catch(Exception exception)
        {
            await Console.Out.WriteLineAsync(Resources.Commands_Init_Failed);

            await Console.Error.WriteLineAsync(string.Format(Resources.Commands_Exceptions_Details, exception.Message));
                 
            return 1;
        }
    }

    private async Task CreateConfigFileAsync(CancellationToken cancellationToken)
    {
        string defaultConfigTomlContents = "";
                
        FileInfo file = new(ConfigFilePath);
        DirectoryInfo directory = file.GetDirectory();
                
        Directory.CreateDirectory(directory.FullName);
            
        await File.WriteAllTextAsync(ConfigFilePath, defaultConfigTomlContents, cancellationToken);
    }

    private async Task CreateAuthorsTomlFileAsync(CancellationToken cancellationToken)
    {
        if (CreateGlobalFile)
        {
            
        }
        else
        {
            
        }
    }
}