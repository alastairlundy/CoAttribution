/*
    CoAuthorCli
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAuthor.Cli.Helpers.Contexts;
using CoAuthor.Cli.Models;
using Tomlyn;

namespace CoAuthor.Cli.Commands;

[CliCommand(Name = "config", Parent = typeof(RootCommand))]
public class ConfigCommand
{
    private readonly IConfiguration _configuration;

    public ConfigCommand(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    [CliArgument(Order = 0,
        AllowedValues = ["authors.global.path",
            "trailers.default", "trailers.assistance", "trailers.coauthorship"],
        Required = true,
        Arity = CliArgumentArity.ExactlyOne)]
    public string Key { get; set; } = "";
  
    [CliArgument(Order = 2, Required = true, Arity = CliArgumentArity.ExactlyOne)]
    public string Value { get; set; } = "";
    
    [CliOption(Name = "config-path", Required = false,
        Arity = CliArgumentArity.ExactlyOne)]
    public string ConfigPath { get; set; } = "";

    [CliCommand(Name = "get", Description = "Get config value.")]
    public async Task<int> GetValueAsync(CancellationToken cancellationToken = default)
    {
        ConfigPath = ConfigurationFileHelper.ResolveConfigFile(_configuration);

        try
        {
            string value = await GetValueAsync(Key, cancellationToken);

            await Console.Out.WriteLineAsync(value);

            return 0;
        }
        catch (KeyNotFoundException)
        {
            await Console.Error.WriteLineAsync(string.Format(Resources.Commands_Config_KeyNotValid, Key));
            return 1;
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync(string.Format(Resources.Commands_Config_EncounteredError_GetValue,
                Key, exception.Message));

            return 1;
        }
    }

    [CliCommand(Name = "set", Description = "Set config value.")]
    public async Task<int> SetValueAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SetValueAsync(Key, Value, cancellationToken);

            await Console.Out.WriteLineAsync($"Set value for {Key}");

            return 0;
        }
        catch (KeyNotFoundException)
        {
            await Console.Error.WriteLineAsync(string.Format(Resources.Commands_Config_KeyNotValid, Key));

            return 1;
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync(string.Format(Resources.Commands_Config_EncounteredError_SetValue, Key,
                exception.Message));
            
            return 1;
        }
    }
    
    public int  Run(CliContext context)
    {
        context.ShowHelp();
        return -1;
    }

    private async Task<string> GetValueAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        
        string text = await File.ReadAllTextAsync(ConfigPath, cancellationToken);
        
        AppConfig? appConfig = TomlSerializer.Deserialize<AppConfig>(text, ConfigSettingsTomlContext.Default);

        if (appConfig is null)
        {
            throw new InvalidOperationException("");
        }

        if (key.StartsWith("path.", StringComparison.CurrentCultureIgnoreCase))
        {
            return appConfig.PathsSettings[key.Replace("path.", string.Empty)];
        }
        if (key.StartsWith("trailers.", StringComparison.CurrentCultureIgnoreCase))
        {
            return appConfig.TrailersSettings[key.Replace("trailers.", string.Empty)];
        }
        if (key.StartsWith("tui.", StringComparison.CurrentCultureIgnoreCase))
        {
            return appConfig.TuiSettings[key.Replace("tui.", string.Empty)];
        }
        if (key.StartsWith("authors_registry.", StringComparison.CurrentCultureIgnoreCase))
        {
            return appConfig.AuthorsRegistry[key.Replace("authors_registry.", string.Empty)];
        }

        throw new KeyNotFoundException();
    }

    private async Task SetValueAsync(string key, string value,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentException.ThrowIfNullOrEmpty(value);
        
        string text = await File.ReadAllTextAsync(ConfigPath, cancellationToken);
        
        AppConfig? appConfig = TomlSerializer.Deserialize<AppConfig>(text, ConfigSettingsTomlContext.Default);

        if (appConfig is null)
        {
            throw new InvalidOperationException("");
        }

        if (key.StartsWith("path.", StringComparison.CurrentCultureIgnoreCase))
        {
            appConfig.PathsSettings[key.Replace("path.", string.Empty)] = value;
        }
        if (key.StartsWith("trailers.", StringComparison.CurrentCultureIgnoreCase))
        {
            appConfig.TrailersSettings[key.Replace("trailers.", string.Empty)] = value;
        }
        if (key.StartsWith("tui.", StringComparison.CurrentCultureIgnoreCase))
        {
            appConfig.TuiSettings[key.Replace("tui.", string.Empty)] = value;
        }
        if (key.StartsWith("authors_registry.", StringComparison.CurrentCultureIgnoreCase))
        {
            appConfig.AuthorsRegistry[key.Replace("authors_registry.", string.Empty)] = value;
        }
        
        text = TomlSerializer.Serialize(appConfig, ConfigSettingsTomlContext.Default);
        
        await File.WriteAllTextAsync(ConfigPath, text, cancellationToken);
    }
}