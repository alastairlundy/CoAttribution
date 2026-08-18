/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Cli.Tui.Abstractions;
using CoAttribution.Cli.Tui.ViewModels;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace CoAttribution.Cli.Tui.Views;

/// <summary>
/// Placeholder view for author selection (TK008).
/// Shows a minimal list of available authors for the user to pick from.
/// </summary>
public sealed class AuthorSelectionView : View, IStatusBarProvider
{
    private readonly AuthorSelectionViewModel _viewModel;

    /// <summary>
    /// Raised when the user confirms their author selection.
    /// </summary>
#pragma warning disable CS0067 // Event is part of the public API for TK008
    public event Action? Confirmed;
#pragma warning restore CS0067

    public AuthorSelectionView(AuthorSelectionViewModel viewModel)
    {
        _viewModel = viewModel;

        Title = "Select Authors";

        Label placeholder = new()
        {
            Text = "Author selection will be implemented in TK008.\nPress Enter to continue.",
            X = Pos.Center(),
            Y = Pos.Center(),
        };

        Add(placeholder);
    }

    public IReadOnlyList<StatusBarKeyBinding> GetKeyBindings() =>
    [
        new(Key.Enter, "Enter confirm"),
        new(Key.Esc, "Esc back"),
    ];
}
