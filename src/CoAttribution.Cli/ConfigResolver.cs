/*  
    CoAttribution  
    Copyright (c) Alastair Lundy 2026  

    This Source Code Form is subject to the terms of the Mozilla Public  
    License, v. 2.0. If a copy of the MPL was not distributed with this  
    file, You can obtain one at https://mozilla.org/MPL/2.0/.  
 */

using Tomlyn;

namespace CoAttribution.Cli;

public class ConfigResolver : IConfigResolver
{
    public async Task<AppConfig> ResolveAppConfig(IConfiguration configuration, CancellationToken cancellationToken)
    {
        string? configFile = configuration["config-file"];

        if (string.IsNullOrEmpty(configFile))
        {
            throw new FileNotFoundException(Resources.Exceptions_FileNotFound_ConfigFile_ToCreate);
        }

        if (!File.Exists(configFile))
        {
            throw new FileNotFoundException(string.Format(Resources.Exceptions_FileNotFound_ConfigFile, configFile));
        }

        string tomlConfigText = await File.ReadAllTextAsync(configFile, cancellationToken);

        AppConfig? appConfig = TomlSerializer.Deserialize<AppConfig>(tomlConfigText, ConfigSettingsTomlContext.Default);

        if (appConfig is null)
        {
            throw new ArgumentException(string.Format(Resources.Exceptions_Arguments_InvalidConfigFileConfiguration, configFile));
        }

        return appConfig;
    }
}
