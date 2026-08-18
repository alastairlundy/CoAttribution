/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Cli.Components.Windows;
using Terminal.Gui.App;

namespace CoAttribution.Cli.Tui.Composition;

/// <summary>
/// Cross-cutting plumbing that initializes Terminal.Gui v2, builds
/// <see cref="MainWindow"/>, applies the <see cref="StatusBarComposer"/>,
/// and runs the application.
/// </summary>
public sealed class TuiCompositionRoot
{
    private readonly IServiceProvider _serviceProvider;

    public TuiCompositionRoot(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Initializes Terminal.Gui v2, resolves <see cref="MainWindow"/>,
    /// applies the status bar, and runs the application.
    /// </summary>
    public async Task<int> LaunchAsync()
    {
        try
        {
            using IApplication app = Application.Create().Init();

            MainWindow mainWindow = new();

            app.Run(mainWindow);

            return await Task.FromResult(0);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);

            return await Task.FromException<int>(exception);
        }
    }
}
