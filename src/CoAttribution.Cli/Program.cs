/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Cli;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

const string appName = "CoAttribution";

string configFilePath = ExtractConfigPath(args) ?? DetermineDefaultConfigFilePath();

IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["config-file"] = configFilePath,
        ["registry-path"] = ExtractRegistryPath(args)
    });

IConfiguration configuration = configurationBuilder.Build();

ServiceCollection services = new();
services.AddCoAttributionServices(configuration);

using ServiceProvider serviceProvider = services.BuildServiceProvider();

// Exactly one RootCommand type is in scope here: the CoAttribution handler
// class above; System.CommandLine's is referenced by its full name.
System.CommandLine.RootCommand rootCommand = RootCommand.Create(serviceProvider);

return await CliRunner.RunAsync(rootCommand, args);


static string? ExtractConfigPath(string[] args)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], "--config-path", StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }
    return null;
}

static string? ExtractRegistryPath(string[] args)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], "--registry", StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }
    return null;
}

static string DetermineDefaultConfigFilePath()
{
    const string configFileName = "config.toml";
    
    if (OperatingSystem.IsWindows())
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appName, configFileName);
    }
    if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Application Support", appName, configFileName);
    }
    if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
    {
        string configDirectory = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ??
                                 Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".config");
            
        return Path.Combine(configDirectory, appName, configFileName);
    }
        
    throw new PlatformNotSupportedException();
}