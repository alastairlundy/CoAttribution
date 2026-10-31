/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using Tomlyn;

namespace CoAttribution.Cli.Commands;

[CliCommand(Name = "config", Alias = "cfg", Parent = typeof(RootCommand))]
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
        AllowedValues = ["authors_registry.paths.global",
            "trailers.default", "trailers.assistance", "trailers.coauthorship"],
        Required = true,
        Arity = CliArgumentArity.ExactlyOne)]
    public string Key { get; set; } = "";
  
    [CliArgument(Order = 2, Required = true, Arity = CliArgumentArity.ExactlyOne)]
    public string Value { get; set; } = "";

    [CliCommand(Name = "get", Description = "Get config value.")]
    public async Task<int> GetValueAsync(CancellationToken cancellationToken = default)
    {
        AppConfig config = await _configResolver.ResolveAppConfig(_configuration, cancellationToken);

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
        AppConfig config = await _configResolver.ResolveAppConfig(_configuration, cancellationToken);
        string configPath = _configuration["config-file"] ?? "";

        try
        {
            await SetValueAsync(config, Key, Value, configPath, cancellationToken);

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
                // Slice the prefix in place: avoids the intermediate string a
                // key.Replace(prefix, "") would allocate per lookup.
                var pathLookup = config.PathsSettings.GetAlternateLookup<ReadOnlySpan<char>>();
                return Task.FromResult(pathLookup[key.AsSpan("path.".Length)]);
            }
            if (key.StartsWith("trailers.", StringComparison.CurrentCultureIgnoreCase))
            {
                var trailersLookup = config.TrailersSettings.GetAlternateLookup<ReadOnlySpan<char>>();
                return Task.FromResult(trailersLookup[key.AsSpan("trailers.".Length)]);
            }
            if (key.StartsWith("tui.", StringComparison.CurrentCultureIgnoreCase))
            {
                var tuiLookup = config.TuiSettings.GetAlternateLookup<ReadOnlySpan<char>>();
                return Task.FromResult(tuiLookup[key.AsSpan("tui.".Length)]);
            }
            if (key.StartsWith("authors_registry.", StringComparison.CurrentCultureIgnoreCase))
            {
                var authorsLookup = config.AuthorsRegistry.GetAlternateLookup<ReadOnlySpan<char>>();
                return Task.FromResult(authorsLookup[key.AsSpan("authors_registry.".Length)]);
            }

            throw new KeyNotFoundException();
        }
        catch (Exception exception)
        {
            return Task.FromException<string>(exception);
        }
    }

    private async Task SetValueAsync(AppConfig? config, string key, string value,
        string configPath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentException.ThrowIfNullOrEmpty(value);
        
        if (config is null)
        {
            throw new InvalidOperationException("");
        }

        if (key.StartsWith("path.", StringComparison.CurrentCultureIgnoreCase))
        {
            config.PathsSettings.GetAlternateLookup<ReadOnlySpan<char>>()[key.AsSpan("path.".Length)] = value;
        }
        if (key.StartsWith("trailers.", StringComparison.CurrentCultureIgnoreCase))
        {
            config.TrailersSettings.GetAlternateLookup<ReadOnlySpan<char>>()[key.AsSpan("trailers.".Length)] = value;
        }
        if (key.StartsWith("tui.", StringComparison.CurrentCultureIgnoreCase))
        {
            config.TuiSettings.GetAlternateLookup<ReadOnlySpan<char>>()[key.AsSpan("tui.".Length)] = value;
        }
        if (key.StartsWith("authors_registry.", StringComparison.CurrentCultureIgnoreCase))
        {
            config.AuthorsRegistry.GetAlternateLookup<ReadOnlySpan<char>>()[key.AsSpan("authors_registry.".Length)] = value;
        }
        
        string text = TomlSerializer.Serialize(config, ConfigSettingsTomlContext.Default);
        
        await File.WriteAllTextAsync(configPath, text, cancellationToken);
    }
}