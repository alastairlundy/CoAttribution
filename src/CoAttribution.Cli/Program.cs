/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.CommandLine;
using CoAttribution.Cli.Helpers;
using Microsoft.Extensions.DependencyInjection;

const string appName = "CoAuthor";

IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
    .AddCommandLine(args)
    .AddEnvironmentVariables();

if (!args.Contains("--config-path", StringComparer.OrdinalIgnoreCase)) 
    configurationBuilder.Properties.Add("config-path", DetermineDefaultConfigFilePath());


IConfiguration configuration = configurationBuilder.Build();

Cli.Ext.ConfigureServices(services =>
{
    services.AddSingleton<ICoAuthorResolver, CoAuthorResolver>();
    services.AddSingleton<IRegistryPathResolver, AppConfigRegistryPathResolver>();
    services.AddSingleton<IAuthorRegistry, AuthorRegistry>();
    services.AddSingleton<ICommitMessageBuilder, CommitMessageBuilder>();
    
    services.AddSingleton(configuration);
});

CliSettings settings = new()
{
    EnablePosixBundling = true,
    EnableDefaultExceptionHandler = true,
    
};

return await Cli.RunAsync<RootCommand>(args, settings);


static string DetermineDefaultConfigFilePath()
{
    if (OperatingSystem.IsWindows())
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appName);
    }
    if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Application Support", appName);
    }
    if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
    {
        string configDirectory = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ??
                                 Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".config");
            
        return Path.Combine(configDirectory, appName);
    }
        
    throw new PlatformNotSupportedException();
}