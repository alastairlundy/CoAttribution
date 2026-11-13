/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.CommandLine;
using System.CommandLine.Help;
using CoAttribution.Cli.Tui.Composition;
using CoAttribution.Cli.Tui.Dialogs;
using CoAttribution.Lib.Abstractions;
using CoAttribution.Lib.Models.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Terminal.Gui.App;

namespace CoAttribution.Cli.Commands;

public class RootCommand
{
    private readonly IAuthorRegistry _authorRegistry;
    private readonly TuiCompositionRoot _compositionRoot;
    private readonly SetupDialog _setupDialog;
    private readonly ILogger<RootCommand> _logger;

    public string ConfigPath { get; set; } = string.Empty;

    public string RegistryPath { get; set; } = string.Empty;

    public RootCommand(IAuthorRegistry authorRegistry, TuiCompositionRoot compositionRoot, SetupDialog setupDialog, ILogger<RootCommand> logger)
    {
        _authorRegistry = authorRegistry;
        _compositionRoot = compositionRoot;
        _setupDialog = setupDialog;
        _logger = logger;
    }

    /// <summary>
    /// Builds the System.CommandLine command tree: the root command with its
    /// recursively visible <c>--config-path</c>/<c>--registry</c> options and
    /// every top-level subcommand. Handlers are created from
    /// <paramref name="services"/> on invocation, mirroring the previous
    /// attribute-based binding.
    /// </summary>
    public static System.CommandLine.RootCommand Create(IServiceProvider services)
    {
        Option<string> configPathOption = new("--config-path")
        {
            Arity = ArgumentArity.ExactlyOne,
            Recursive = true
        };

        Option<string> registryOption = new("--registry")
        {
            Arity = ArgumentArity.ExactlyOne,
            Recursive = true
        };

        System.CommandLine.RootCommand root = new();
        root.Options.Add(configPathOption);
        root.Options.Add(registryOption);
        root.Subcommands.Add(AuthorRootCommand.Create(services));
        root.Subcommands.Add(CommitCommand.Create(services));
        root.Subcommands.Add(ConfigCommand.Create(services));
        root.Subcommands.Add(InitCommand.Create(services));
        root.Subcommands.Add(MessageCommand.Create(services));

        // Wrap the built-in help action so every help render carries the
        // shipped version/copyright banner and command title ahead of the
        // framework text (see BannerHelpAction).
        foreach (Option option in root.Options)
        {
            if (option is HelpOption { Action: HelpAction helpAction })
            {
                option.Action = new BannerHelpAction(helpAction);
            }
        }

        root.SetAction(async (parseResult, cancellationToken) =>
        {
            RootCommand handler = ActivatorUtilities.CreateInstance<RootCommand>(services);
            handler.ConfigPath = parseResult.GetValue(configPathOption) ?? string.Empty;
            handler.RegistryPath = parseResult.GetValue(registryOption) ?? string.Empty;

            return await handler.RunAsync(CliContext.From(parseResult, cancellationToken));
        });

        return root;
    }

    public async Task<int> RunAsync(CliContext context)
    {
        // Non-TTY: print help and exit 0
        if (Console.IsOutputRedirected || Console.IsInputRedirected)
        {
            context.ShowHelp();
            return 0;
        }

        // Empty registry: show SetupDialog first
        GitCoAuthorConfig config = await _authorRegistry.GetAuthorConfigAsync(CancellationToken.None);
        if (config.Agents.Count == 0 && config.Humans.Count == 0)
        {
            bool authorAdded = false;

            try
            {
                ThemeConfigurationHelper.ApplyTheme();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not apply CoAttribution theme — falling back to defaults");
            }

            using IApplication app = Application.Create().Init();

            void OnAuthorAdded() => authorAdded = true;
            void OnCancelled() => app.RequestStop();

            _setupDialog.AuthorAdded += OnAuthorAdded;
            _setupDialog.Cancelled += OnCancelled;

            try
            {
                app.Run(_setupDialog);
            }
            finally
            {
                _setupDialog.AuthorAdded -= OnAuthorAdded;
                _setupDialog.Cancelled -= OnCancelled;
            }

            if (!authorAdded)
                return 0;
        }

        // Launch TUI
        return await _compositionRoot.LaunchAsync();
    }
}