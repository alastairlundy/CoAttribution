/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Cli.Tui.Composition;
using CoAttribution.Cli.Tui.Dialogs;
using CoAttribution.Lib.Abstractions;
using CoAttribution.Lib.Models.DTOs;
using Terminal.Gui.App;

namespace CoAttribution.Cli.Commands;

[CliCommand(ShortFormAutoGenerate = CliNameAutoGenerate.None)]
public class RootCommand
{
    private readonly IAuthorRegistry _authorRegistry;
    private readonly TuiCompositionRoot _compositionRoot;
    private readonly SetupDialog _setupDialog;

    [CliOption(Name = "config-path", Required = false, Arity = CliArgumentArity.ExactlyOne, Recursive = true)]
    public string ConfigPath { get; set; } = string.Empty;

    public RootCommand(IAuthorRegistry authorRegistry, TuiCompositionRoot compositionRoot, SetupDialog setupDialog)
    {
        _authorRegistry = authorRegistry;
        _compositionRoot = compositionRoot;
        _setupDialog = setupDialog;
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
#pragma warning disable CS0618 // Static Application API — will migrate to IApplication in TuiCompositionRoot
            bool authorAdded = false;

            void OnAuthorAdded() => authorAdded = true;
            void OnCancelled() => Application.RequestStop();

            _setupDialog.AuthorAdded += OnAuthorAdded;
            _setupDialog.Cancelled += OnCancelled;

            try
            {
                Application.Run(_setupDialog);
            }
            finally
            {
                _setupDialog.AuthorAdded -= OnAuthorAdded;
                _setupDialog.Cancelled -= OnCancelled;
            }

            if (!authorAdded)
                return 0;
#pragma warning restore CS0618
        }

        // Launch TUI
        return await _compositionRoot.LaunchAsync();
    }
}