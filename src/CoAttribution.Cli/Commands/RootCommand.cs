/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Cli.Tui.Composition;
using CoAttribution.Lib.Abstractions;
using CoAttribution.Lib.Models.DTOs;

namespace CoAttribution.Cli.Commands;

[CliCommand(ShortFormAutoGenerate = CliNameAutoGenerate.None)]
public class RootCommand
{
    private readonly IAuthorRegistry _authorRegistry;
    private readonly TuiCompositionRoot _compositionRoot;

    public RootCommand(IAuthorRegistry authorRegistry, TuiCompositionRoot compositionRoot)
    {
        _authorRegistry = authorRegistry;
        _compositionRoot = compositionRoot;
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
            // SetupDialog will be fully implemented in TK009
        }

        // Launch TUI
        return await _compositionRoot.LaunchAsync();
    }
}