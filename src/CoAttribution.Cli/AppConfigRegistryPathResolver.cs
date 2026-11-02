/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using Tomlyn;

namespace CoAttribution.Cli;

public class AppConfigRegistryPathResolver : IRegistryPathResolver
{
    private readonly IConfiguration _configuration;
    private string? _cachedPath;
    private bool _resolved;

    public AppConfigRegistryPathResolver(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string?> GetGlobalRegistryPathAsync(CancellationToken cancellationToken)
    {
        if (_resolved)
            return await Task.FromResult(_cachedPath);

        string? configFile = _configuration["config-file"];

        if (string.IsNullOrEmpty(configFile) || !File.Exists(configFile))
        {
            _resolved = true;
            return await Task.FromResult<string?>(null);
        }
        
        string configText = await File.ReadAllTextAsync(configFile, cancellationToken);

        AppConfig? appConfig = TomlSerializer.Deserialize<AppConfig>(configText, ConfigSettingsTomlContext.Default);

        if (appConfig is null)
        {
            _resolved = true;
            return await Task.FromResult<string?>(null);
        }

        _cachedPath = appConfig.PathsSettings.GetValueOrDefault("global_registry");
        _resolved = true;

        return await Task.FromResult(_cachedPath);
    }

    /// <inheritdoc />
    public async Task<string?> GetExplicitRegistryPathAsync(CancellationToken cancellationToken)
    {
        string? explicitPath = _configuration["registry-path"];

        if (string.IsNullOrWhiteSpace(explicitPath))
            return await Task.FromResult<string?>(null);

        // The explicit override always wins over the configured global registry.
        return await Task.FromResult(explicitPath);
    }
}
