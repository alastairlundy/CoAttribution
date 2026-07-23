/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Cli;
using CoAttribution.Lib;
using CoAttribution.Lib.Abstractions;
using CliInvoke.Extensions;
using Microsoft.Extensions.DependencyInjection;

const string appName = "CoAuthor";

var switchMappings = new Dictionary<string, string>
{
    { "--config-path", "config-file" }
};

IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
    .AddCommandLine(args, switchMappings)
    .AddEnvironmentVariables();

if (!args.Contains("--config-path", StringComparer.OrdinalIgnoreCase)) 
    configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["config-file"] = DetermineDefaultConfigFilePath()
    });


IConfiguration configuration = configurationBuilder.Build();

Cli.Ext.ConfigureServices(services =>
{
    services.AddCliInvoke();
    services.AddSingleton<IRegistryPathResolver, AppConfigRegistryPathResolver>();
    services.AddSingleton<IConfigResolver, ConfigResolver>();
    services.AddSingleton<IAuthorRegistry, AuthorRegistry>();
    services.AddSingleton<ICommitMessageBuilder, CommitMessageBuilder>();
    services.AddSingleton<ICommitOrchestrator, CommitOrchestrator>();
    services.AddSingleton<IGitClient, CliGitClient>();
    
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