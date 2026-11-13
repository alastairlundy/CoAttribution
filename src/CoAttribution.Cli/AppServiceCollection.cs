/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Extensions;
using CoAttribution.Cli.Tui;
using CoAttribution.Cli.Tui.Composition;
using CoAttribution.Cli.Tui.Dialogs;
using CoAttribution.Cli.Tui.ViewModels;
using CoAttribution.Cli.Tui.Views;
using CoAttribution.Lib.HostResolution;
using CoAttribution.Lib.HostResolution.Abstractions;
using CoAttribution.Lib.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Tomlyn;

namespace CoAttribution.Cli;

/// <summary>
/// Composition root for the CLI: registers every service the command tree and
/// the TUI resolve, then hands the built container to the System.CommandLine
/// command tree.
/// </summary>
/// <remarks>
/// Kept out of <c>Program.cs</c> so tests can build the real container and
/// assert that the dependency graphs resolve. <see cref="AppConfig"/> is a
/// constructor dependency of <see cref="HostResolver"/>, which commit,
/// message, and the TUI all reach — a missing registration there breaks all
/// three at resolution time.
/// </remarks>
public static class AppServiceCollection
{
    /// <summary>
    /// Registers the application services against <paramref name="configuration"/>
    /// and returns the collection for chaining.
    /// </summary>
    public static ServiceCollection AddCoAttributionServices(this ServiceCollection services, IConfiguration configuration)
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

        // AppConfig is a constructor dependency of HostResolver, so it must be
        // resolvable for commit, message, and the TUI. The factory keeps the
        // read lazy (it runs the first time a consumer resolves AppConfig, not
        // during container setup) and falls back to empty defaults when
        // config.toml does not exist yet, so commands that do not depend on it
        // keep working. ConfigCommand still resolves the file on demand
        // through IConfigResolver.
        services.AddSingleton(_ => LoadAppConfig(configuration));

        // TUI services — resolution deferred to RootCommand handler
        services.AddSingleton(_ =>
        {
            // Load the TUI config once from its embedded resource (startup-only,
            // trimming/AoT-safe) and expose the Glyphs section to a reflection-free GlyphSet.
            Assembly assembly = typeof(AppServiceCollection).Assembly;
            using Stream? stream = assembly.GetManifestResourceStream("Resources.config.json")
                ?? throw new InvalidOperationException("Embedded resource 'Resources.config.json' was not found.");
            IConfiguration resourceConfig = new ConfigurationBuilder().AddJsonStream(stream).Build();
            return GlyphSet.FromConfiguration(resourceConfig.GetSection("Glyphs"));
        });
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

        return services;
    }

    /// <summary>
    /// Reads <c>config.toml</c> for the DI-resolved <see cref="AppConfig"/>.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="ConfigResolver.ResolveAppConfig"/>, a missing file is
    /// not an error here: consumers such as <see cref="HostResolver"/> only
    /// need optional settings (host aliases) and must not start failing just
    /// because the user has not run <c>init</c> yet. A present-but-malformed
    /// config is still surfaced as an error, exactly as ConfigResolver does.
    /// </remarks>
    private static AppConfig LoadAppConfig(IConfiguration configuration)
    {
        string? configFile = configuration["config-file"];

        if (string.IsNullOrEmpty(configFile) || !File.Exists(configFile))
        {
            return new AppConfig();
        }

        string configText = File.ReadAllText(configFile);

        return TomlSerializer.Deserialize<AppConfig>(configText, ConfigSettingsTomlContext.Default)
               ?? throw new ArgumentException(
                   string.Format(Resources.Exceptions_Arguments_InvalidConfigFileConfiguration, configFile));
    }
}
