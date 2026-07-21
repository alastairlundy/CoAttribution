/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using Tomlyn;

namespace CoAttribution.Cli.Commands;

[CliCommand(Name = "config", Parent = typeof(RootCommand))]
public class ConfigCommand
{
    private readonly IConfiguration _configuration;
    private readonly IConfigResolver _configResolver;

    public ConfigCommand(IConfiguration configuration, IConfigResolver configResolver)
    {
        _configuration = configuration;
        _configResolver = configResolver;
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
        AppConfig? config;
        
        if (string.IsNullOrEmpty(ConfigPath))
        {
            config = await _configResolver.ResolveAppConfig(_configuration, cancellationToken);
            ConfigPath = _configuration["config-path"] ?? _configuration["coauthor_config_file"] ?? "";
        }
        else
        {
            string text = await File.ReadAllTextAsync(ConfigPath, cancellationToken);
        
            config = TomlSerializer.Deserialize<AppConfig>(text, ConfigSettingsTomlContext.Default);
        }

        try
        {
            string value = await GetValue(config, Key);

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
        AppConfig? config;
        
        if (string.IsNullOrEmpty(ConfigPath))
        {
            config = await _configResolver.ResolveAppConfig(_configuration, cancellationToken);
            ConfigPath = _configuration["config-path"] ?? _configuration["coauthor_config_file"] ?? "";
        }
        else
        {
            string text = await File.ReadAllTextAsync(ConfigPath, cancellationToken);
        
            config = TomlSerializer.Deserialize<AppConfig>(text, ConfigSettingsTomlContext.Default);
        }

        try
        {
            await SetValueAsync(config, Key, Value, cancellationToken);

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

    private static Task<string> GetValue(AppConfig? config, string key)
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
        
            if (config is null)
            {
                throw new InvalidOperationException("");
            }

            if (key.StartsWith("path.", StringComparison.CurrentCultureIgnoreCase))
            {
                return Task.FromResult(config.PathsSettings[key.Replace("path.", string.Empty)]);
            }
            if (key.StartsWith("trailers.", StringComparison.CurrentCultureIgnoreCase))
            {
                return Task.FromResult(config.TrailersSettings[key.Replace("trailers.", string.Empty)]);
            }
            if (key.StartsWith("tui.", StringComparison.CurrentCultureIgnoreCase))
            {
                return Task.FromResult(config.TuiSettings[key.Replace("tui.", string.Empty)]);
            }
            if (key.StartsWith("authors_registry.", StringComparison.CurrentCultureIgnoreCase))
            {
                return Task.FromResult(config.AuthorsRegistry[key.Replace("authors_registry.", string.Empty)]);
            }

            throw new KeyNotFoundException();
        }
        catch (Exception exception)
        {
            return Task.FromException<string>(exception);
        }
    }

    private async Task SetValueAsync(AppConfig? config, string key, string value,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentException.ThrowIfNullOrEmpty(value);
        
        if (config is null)
        {
            throw new InvalidOperationException("");
        }

        if (key.StartsWith("path.", StringComparison.CurrentCultureIgnoreCase))
        {
            config.PathsSettings[key.Replace("path.", string.Empty)] = value;
        }
        if (key.StartsWith("trailers.", StringComparison.CurrentCultureIgnoreCase))
        {
            config.TrailersSettings[key.Replace("trailers.", string.Empty)] = value;
        }
        if (key.StartsWith("tui.", StringComparison.CurrentCultureIgnoreCase))
        {
            config.TuiSettings[key.Replace("tui.", string.Empty)] = value;
        }
        if (key.StartsWith("authors_registry.", StringComparison.CurrentCultureIgnoreCase))
        {
            config.AuthorsRegistry[key.Replace("authors_registry.", string.Empty)] = value;
        }
        
        string text = TomlSerializer.Serialize(config, ConfigSettingsTomlContext.Default);
        
        await File.WriteAllTextAsync(ConfigPath, text, cancellationToken);
    }
}