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

namespace CoAuthor.Cli.Helpers;

public class FileHelper
{
    public static FileInfo ResolveConfigFile(IConfiguration configuration)
    {
        string? configFile = configuration["config-file"] ?? configuration["coauthor_config_file"];

        configFile ??= "";
        
        return string.IsNullOrEmpty(configFile) ? 
            throw new InvalidOperationException(string.Format(Resources.Exceptions_CouldNotFindConfigFile, configFile)) 
            : new FileInfo(configFile);
    }

    public static async Task<FileInfo> ResolveAuthorTomlFileAsync(FileInfo configFile, CancellationToken cancellationToken = default)
    {
        FileInfo? localAuthorsFile = await TryResolveLocalAuthorsFileAsync(cancellationToken);

        if (localAuthorsFile is not null)
            return localAuthorsFile;
        
        if (!configFile.Exists)
            throw new FileNotFoundException(string.Format(Resources.Exceptions_CouldNotFindConfigFile, configFile.FullName));

        string tomlConfigText = await File.ReadAllTextAsync(configFile.FullName, cancellationToken);

        AppConfig? config = TomlSerializer.Deserialize<AppConfig>(tomlConfigText, ConfigSettingsTomlContext.Default);

        if (config is null)
            throw new ArgumentException("Config file path is not a valid TOML configuration file");

        bool success = config.PathsSettings.TryGetValue("global_registry", out string? authorsFile);

        if (!success || authorsFile is null)
        {
            throw new 
        }

        return new  FileInfo(authorsFile);
    }

    private static async Task<FileInfo?> TryResolveLocalAuthorsFileAsync(CancellationToken cancellationToken = default)
    {
        
    }
}