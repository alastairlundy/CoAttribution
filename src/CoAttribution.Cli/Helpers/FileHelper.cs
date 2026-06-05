/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using Tomlyn;
using ConfigSettingsTomlContext = CoAuthor.Cli.Helpers.Contexts.ConfigSettingsTomlContext;

namespace CoAttribution.Cli.Helpers;

public static class FileHelper
{
    public static async Task<FileInfo> ResolveAuthorTomlFileAsync(FileInfo configFile, CancellationToken cancellationToken = default)
    {
        FileInfo? localAuthorsFile = await TryResolveLocalAuthorsFile();

        if (localAuthorsFile is not null)
            return localAuthorsFile;
        
        if (!configFile.Exists)
            throw new FileNotFoundException(string.Format(Resources.Exceptions_FileNotFound_ConfigFile, configFile.FullName));

        string tomlConfigText = await File.ReadAllTextAsync(configFile.FullName, cancellationToken);

        AppConfig? config = TomlSerializer.Deserialize<AppConfig>(tomlConfigText, ConfigSettingsTomlContext.Default);

        if (config is null)
            throw new ArgumentException(string.Format(Resources.Exceptions_Arguments_InvalidConfigFileConfiguration, configFile.FullName));

        bool success = config.PathsSettings.TryGetValue("global_registry", out string? authorsFile);

        if (!success || authorsFile is null)
            throw new FileNotFoundException(Resources.Exceptions_FileNotFound_AuthorsFile);

        return new  FileInfo(authorsFile);
    }

    private static Task<FileInfo?> TryResolveLocalAuthorsFile()
    {
        DirectoryInfo directoryInfo = new(Directory.GetCurrentDirectory());

        FileInfo? file = directoryInfo
            .EnumerateFiles("*.toml", new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                MaxRecursionDepth = 5,
                MatchCasing = MatchCasing.CaseInsensitive,
            })
            .FirstOrDefault(f => f.Name.Equals("authors.toml", StringComparison.CurrentCultureIgnoreCase));
        
        return Task.FromResult(file);
    }
}