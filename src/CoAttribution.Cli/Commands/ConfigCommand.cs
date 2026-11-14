/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Tomlyn;

namespace CoAttribution.Cli.Commands;

public class ConfigCommand
{
    private readonly IConfiguration _configuration;
    private readonly IConfigResolver _configResolver;

    public ConfigCommand(IConfiguration configuration, IConfigResolver configResolver)
    {
        _configuration = configuration;
        _configResolver = configResolver;
    }
  
    public string Key { get; set; } = "";
  
    public string Value { get; set; } = "";

    /// <summary>
    /// Builds the <c>config</c> subcommand with its positional key/value
    /// arguments. There are no nested subcommands; invoking <c>config</c>
    /// with a key and value renders help and exits with the historical
    /// non-zero code (see <see cref="Run"/>).
    /// </summary>
    public static Command Create(IServiceProvider services)
    {
        Argument<string> keyArgument = new("key");
        Argument<string> valueArgument = new("value");
        keyArgument.AcceptOnlyFromAmong(
            "authors_registry.paths.global");

        Command command = new("config");
        command.Aliases.Add("cfg");
        command.Arguments.Add(keyArgument);
        command.Arguments.Add(valueArgument);

        command.SetAction(parseResult =>
        {
            ConfigCommand handler = ActivatorUtilities.CreateInstance<ConfigCommand>(services);
            handler.Key = parseResult.GetValue(keyArgument) ?? "";
            handler.Value = parseResult.GetValue(valueArgument) ?? "";

            return handler.Run(CliContext.From(parseResult, CancellationToken.None));
        });

        return command;
    }

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
        else if (key.StartsWith("tui.", StringComparison.CurrentCultureIgnoreCase))
        {
            config.TuiSettings.GetAlternateLookup<ReadOnlySpan<char>>()[key.AsSpan("tui.".Length)] = value;
        }
        else if (key.StartsWith("authors_registry.", StringComparison.CurrentCultureIgnoreCase))
        {
            config.AuthorsRegistry.GetAlternateLookup<ReadOnlySpan<char>>()[key.AsSpan("authors_registry.".Length)] = value;
        }
        else
        {
            // Removed keys (trailers.*) and unknown keys report the standard
            // invalid-key error and never reach the file write below.
            throw new KeyNotFoundException();
        }

        string text = TomlSerializer.Serialize(config, ConfigSettingsTomlContext.Default);
        
        await File.WriteAllTextAsync(configPath, text, cancellationToken);
    }
}