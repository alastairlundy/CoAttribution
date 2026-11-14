/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Collections.Concurrent;
using Tomlyn;
using Tomlyn.Serialization;

namespace CoAttribution.Cli.DataAccess;

// ReSharper disable once PartialTypeWithSinglePart
[TomlSerializable(typeof(AppConfig))]
public partial class ConfigSettingsTomlContext : TomlSerializerContext
{
    // Every config read in a process run resolves the same config-file path,
    // so de-duplicating per path yields exactly one warning line per run while
    // staying isolated when tests point at different temp files.
    private static readonly ConcurrentDictionary<string, byte> StaleTrailersWarnedPaths =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Deserializes config TOML through the source-generated context and, when
    /// the config carries a non-empty <c>[trailers]</c> section, emits the
    /// one-line migration warning naming <paramref name="configFilePath"/> -
    /// at most once per config path per process run. The config file is never
    /// modified.
    /// </summary>
    /// <remarks>
    /// This is the single detection point every config-reading path goes
    /// through (<c>ConfigResolver</c>, the DI <c>AppConfig</c> factory, and the
    /// registry-path resolver), so a run prints exactly one warning line no
    /// matter how many times config is read. Detection is a plain dictionary
    /// count on the source-generated model - reflection-free, per the
    /// NativeAOT constraint.
    /// </remarks>
    public static AppConfig? DeserializeConfig(string tomlText, string configFilePath)
    {
        AppConfig? config = TomlSerializer.Deserialize<AppConfig>(tomlText, Default);

        if (config is { TrailersSettings.Count: > 0 }
            && StaleTrailersWarnedPaths.TryAdd(configFilePath, 0))
        {
            Console.Error.WriteLine(string.Format(
                Resources.Commands_Config_TrailersSectionIgnored, configFilePath));
        }

        return config;
    }
}
