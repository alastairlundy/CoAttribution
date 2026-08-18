/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Cli.Tui.Composition;

/// <summary>
/// Cross-cutting plumbing that will initialize Terminal.Gui v2 and run
/// the application. Currently a stub — full implementation in TK005–TK013.
/// </summary>
public sealed class TuiCompositionRoot
{
    private readonly IServiceProvider _serviceProvider;

    public TuiCompositionRoot(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Launches the TUI application. Stub until v2 components are wired up.
    /// </summary>
    public async Task<int> LaunchAsync()
    {
        // TODO: Terminal.Gui v2 application setup (TK005–TK013)
        return await Task.FromResult(0);
    }
}
