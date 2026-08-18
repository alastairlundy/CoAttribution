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

namespace CoAttribution.Cli.Tui.Dialogs;

/// <summary>
/// Placeholder quit dialog (TK013).
/// Offers Save draft / Discard / Cancel when the user presses Esc or Ctrl+C
/// with an in-progress commit form.
/// </summary>
public sealed class QuitDialog : Window, IStatusBarProvider
{
    private readonly CommitFormViewModel _formViewModel;
    private readonly DraftStore _draftStore;

    /// <summary>
    /// Raised when the user chooses to save a draft.
    /// </summary>
#pragma warning disable CS0067 // Event is part of the public API for TK013
    public event Action? DraftSaved;
#pragma warning restore CS0067

    /// <summary>
    /// Raised when the user chooses to discard and close.
    /// </summary>
#pragma warning disable CS0067 // Event is part of the public API for TK013
    public event Action? Discarded;
#pragma warning restore CS0067

    /// <summary>
    /// Raised when the user cancels the quit and returns to the form.
    /// </summary>
#pragma warning disable CS0067 // Event is part of the public API for TK013
    public event Action? Cancelled;
#pragma warning restore CS0067

    public QuitDialog(CommitFormViewModel formViewModel, DraftStore draftStore)
    {
        _formViewModel = formViewModel;
        _draftStore = draftStore;

        Title = "Quit?";

        Label placeholder = new()
        {
            Text = "Quit dialog will be implemented in TK013.\nPress S to save draft, D to discard, Esc to cancel.",
            X = Pos.Center(),
            Y = Pos.Center(),
        };

        Add(placeholder);
    }

    public IReadOnlyList<StatusBarKeyBinding> GetKeyBindings() =>
    [
        new(Key.Esc, "Esc cancel"),
    ];
}
