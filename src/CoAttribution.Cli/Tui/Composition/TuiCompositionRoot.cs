/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Cli.Tui.Views;
using Microsoft.Extensions.DependencyInjection;
using Terminal.Gui.App;
using Terminal.Gui.Views;

namespace CoAttribution.Cli.Tui.Composition;

/// <summary>
/// Cross-cutting plumbing that initializes Terminal.Gui v2 and runs
/// the main TUI application. Resolves <see cref="MainWindow"/> from the
/// shared DI container, applies <see cref="StatusBarComposer"/>, and
/// manages the application lifecycle.
/// </summary>
public sealed class TuiCompositionRoot
{
    private readonly IServiceProvider _serviceProvider;

    public TuiCompositionRoot(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Initializes Terminal.Gui v2, builds <see cref="MainWindow"/> with
    /// its status bar, and runs the application until the user exits.
    /// </summary>
    public async Task<int> LaunchAsync()
    {
        using IApplication app = Application.Create().Init();

        MainWindow mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Initialize();

        StatusBar statusBar = StatusBarComposer.Build(mainWindow);
        mainWindow.Add(statusBar);

        app.Run(mainWindow);

        return await Task.FromResult(0);
    }
}
