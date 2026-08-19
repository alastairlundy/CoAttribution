/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Cli;
using CoAttribution.Cli.Tui;
using CoAttribution.Cli.Tui.Composition;
using CoAttribution.Cli.Tui.Dialogs;
using CoAttribution.Cli.Tui.ViewModels;
using CoAttribution.Cli.Tui.Views;
using CliInvoke.Extensions;
using CoAttribution.Lib.HostResolution;
using CoAttribution.Lib.HostResolution.Abstractions;
using CoAttribution.Lib.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

const string appName = "CoAttribution";

string configFilePath = ExtractConfigPath(args) ?? DetermineDefaultConfigFilePath();

IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["config-file"] = configFilePath
    });

IConfiguration configuration = configurationBuilder.Build();

Cli.Ext.ConfigureServices(services =>
{
    services.AddCliInvoke();
    services.AddLogging(builder =>
    {
        builder.AddProvider(new FileLoggerProvider(FileLogger.GetDefaultLogDirectory()));
    });
    services.AddSingleton<IHostResolver, HostResolver>();
    services.AddSingleton<IRegistryPathResolver, AppConfigRegistryPathResolver>();
    services.AddSingleton<IConfigResolver, ConfigResolver>();
    services.AddSingleton<IAuthorRegistry, AuthorRegistry>();
    services.AddSingleton<ICommitMessageBuilder, CommitMessageBuilder>();
    services.AddSingleton<ICommitOrchestrator, CommitOrchestrator>();
    services.AddSingleton<IGitClient, CliGitClient>();
    services.AddSingleton<IGitConfigClient, GitConfigClient>();
    services.AddSingleton<IGitRemoteProbe, GitRemoteProbe>();
    services.AddSingleton<IRepositoryContext, RepositoryContext>();
    services.AddSingleton<HostBlockWriter>();
    
    services.AddSingleton(configuration);
    
    services.AddSingleton<AppConfig>(sp =>
    {
        IConfigResolver configResolver = sp.GetRequiredService<IConfigResolver>();
        IConfiguration cfg = sp.GetRequiredService<IConfiguration>();
        return configResolver.ResolveAppConfig(cfg, CancellationToken.None).GetAwaiter().GetResult();
    });

    // TUI services — resolution deferred to RootCommand handler
    services.AddSingleton<TuiCompositionRoot>();
    services.AddSingleton<AuthorSelectionViewModel>(sp => new AuthorSelectionViewModel(
        sp.GetRequiredService<IAuthorRegistry>(),
        sp.GetRequiredService<IHostResolver>()));
    services.AddSingleton<CommitFormViewModel>();
    services.AddSingleton<DraftStore>();
    services.AddTransient<CommitFormView>();
    services.AddTransient<AuthorSelectionView>();
    services.AddTransient<PreviewModal>();
    services.AddTransient<QuitDialog>();
    services.AddTransient<SetupDialog>();
    services.AddTransient<MainWindow>();
});

CliSettings settings = new()
{
    EnablePosixBundling = true,
    EnableDefaultExceptionHandler = true,
};

return await Cli.RunAsync<RootCommand>(args, settings);


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