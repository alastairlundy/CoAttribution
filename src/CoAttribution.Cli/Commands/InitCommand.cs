/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Reflection;

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
    
    [CliOption(Name = "global", Alias = "g", Required = false, Arity = CliArgumentArity.ZeroOrOne)]
    public bool CreateGlobalFile { get; set; } = true;
    
    public async Task<int> RunAsync(CliContext cliContext)
    {
        string configFilePath = _configuration["config-file"] ?? "";

        try
        {
            if (!string.IsNullOrEmpty(configFilePath) && !File.Exists(configFilePath))
            {
                await CreateConfigFileAsync(configFilePath, cliContext.CancellationToken);
            }
            
            await Console.Out.WriteLineAsync(string.Format(Resources.Commands_Init_ConfigFileCreated, configFilePath));
            
            string authorsFilePath = await CreateAuthorsTomlFileAsync(cliContext.CancellationToken);
            await Console.Out.WriteLineAsync(string.Format(Resources.Commands_Init_AuthorsFileCreated, authorsFilePath));
            
            return 0;
        }
        catch(Exception exception)
        {
            await Console.Out.WriteLineAsync(Resources.Commands_Init_Failed);

            await Console.Error.WriteLineAsync(string.Format(Resources.Commands_Exceptions_Details, exception.Message));
                 
            return 1;
        }
    }

    private async Task CreateConfigFileAsync(string configFilePath, CancellationToken cancellationToken)
    {
        string defaultConfigTomlContents = "";
                
        string? directoryPath = Path.GetDirectoryName(configFilePath);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
            
        await File.WriteAllTextAsync(configFilePath, defaultConfigTomlContents, cancellationToken);
    }

    private async Task<string> CreateAuthorsTomlFileAsync(CancellationToken cancellationToken)
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

        return targetPath;
    }
}