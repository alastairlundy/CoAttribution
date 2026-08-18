/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Cli.Tui.Abstractions;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace CoAttribution.Cli.Tui.Composition;

/// <summary>
/// Builds a Terminal.Gui v2 <see cref="StatusBar"/> pinned to the bottom of the
/// viewport from the bindings a screen returns via <see cref="IStatusBarProvider"/>.
/// </summary>
public static class StatusBarComposer
{
    /// <summary>
    /// Creates a <see cref="StatusBar"/> whose entries are derived from
    /// <paramref name="provider"/> and positioned at the bottom of the screen.
    /// </summary>
    public static StatusBar Build(IStatusBarProvider provider)
    {
        IReadOnlyList<StatusBarKeyBinding> bindings = provider.GetKeyBindings();

        List<Shortcut> shortcuts = [];

        foreach (StatusBarKeyBinding binding in bindings)
        {
            shortcuts.Add(new Shortcut(binding.Key, binding.Label, null, null));
        }

        StatusBar statusBar = new(shortcuts)
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
        };

        return statusBar;
    }
}
