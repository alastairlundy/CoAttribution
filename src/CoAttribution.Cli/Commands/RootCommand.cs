/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

#if TUI
using Terminal.Gui.App;
using Terminal.Gui.Views;
using CoAttribution.Cli.Components.Windows;
#endif

namespace CoAttribution.Cli.Commands;

[CliCommand(ShortFormAutoGenerate = CliNameAutoGenerate.None)]
public class RootCommand
{
    public Task<int> RunAsync(CliContext context)
    {
#if TUI
        try
        {
            using IApplication app = Application.Create().Init();

            app.Run<MainWindow>();

            return Task.FromResult(0);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);

            return Task.FromException<int>(exception);
        }
#else
        context.ShowHelp();
        return Task.FromResult(1);
#endif
    }
}