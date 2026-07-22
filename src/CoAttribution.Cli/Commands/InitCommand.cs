/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Reflection;
using DotExtensions.IO.Directories;

namespace CoAttribution.Cli.Commands;

[CliCommand(Name = "init", Parent = typeof(RootCommand))]
public class InitCommand
{
    private readonly IConfiguration _configuration;
    private readonly IRegistryPathResolver _pathResolver;

    public InitCommand(IConfiguration configuration, IRegistryPathResolver pathResolver)
    {
        _configuration = configuration;
        _pathResolver = pathResolver;
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
            ConfigFilePath = _configuration["config-file"] ?? "";
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
        string targetPath;

        if (CreateGlobalFile)
        {
            string? globalPath = await _pathResolver.GetGlobalRegistryPathAsync(cancellationToken);

            targetPath = globalPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CoAttribution",
                "authors.toml");
        }
        else
        {
            targetPath = Path.Combine(Environment.CurrentDirectory, ".coauthor", "authors.toml");
        }

        string? directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using Stream? resourceStream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("DEFAULT_AUTHORS.toml");

        if (resourceStream is not null)
        {
            await using FileStream fileStream = File.Create(targetPath);
            await resourceStream.CopyToAsync(fileStream, cancellationToken);
        }
        else
        {
            await File.WriteAllTextAsync(targetPath, string.Empty, cancellationToken);
        }
    }
}