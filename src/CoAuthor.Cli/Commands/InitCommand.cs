/*
    CoAuthorCli
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using DotExtensions.IO.Directories;
/*using Terminal.Gui.App;
 using CoAuthor.Cli.Components.Dialogs;
*/

namespace CoAuthor.Cli.Commands;

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
    
    [CliOption(Name = "config-path", Required = false,
        Arity = CliArgumentArity.ExactlyOne)]
    public string ConfigFilePath { get; set; } = string.Empty;
    
    public async Task<int> RunAsync(CliContext cliContext)
    {
        ConfigFilePath = FileHelper.ResolveConfigFile(_configuration).FullName;
        
        /*if (Interactive)
        {
            IApplication application = Application.Create().Init();

            application = await application.RunAsync<SetupDialog>(CancellationToken.None);

            application.RequestStop();
            
            /*bool exitedSuccess = application  ? 0 : 1;#1#
            /*return exitedSuccess;#1#
            //TODO: Replace with actual code
            return 0;
        }*/

        try
        {
            await CreateConfigFileAsync(cliContext.CancellationToken);
            
            //TODO Move to Resx
            await Console.Out.WriteLineAsync($"Configuration file created at: {ConfigFilePath}");
            
            await CreateDefaultAuthorsTomlFileAsync(cliContext.CancellationToken);
            
            return 0;
        }
        catch(Exception exception)
        {
            await Console.Out.WriteLineAsync(Resources.Commands_Init_Failed);

            await Console.Error.WriteLineAsync($"Exception Details: {exception.Message}");
                
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

    private async Task CreateDefaultAuthorsTomlFileAsync(CancellationToken cancellationToken)
    {
        
    }
}