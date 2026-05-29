/*
    CoAuthorCli
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAuthor.Cli.Components.Windows;
using Terminal.Gui.App;

namespace CoAuthor.Cli.Commands;

[CliCommand(Name = "")]
public class RootCommand
{
    public Task<int> RunAsync(CliContext context)
    {
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
    }
}