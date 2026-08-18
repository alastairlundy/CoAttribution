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
/// Quit dialog shown when the user presses Esc or Ctrl+C with an in-progress
/// commit form. Offers Save draft / Discard / Cancel.
/// </summary>
public sealed class QuitDialog : Window, IStatusBarProvider
{
    private readonly CommitFormViewModel _formViewModel;
    private readonly DraftStore _draftStore;
    private readonly Button _saveDraftButton;
    private readonly Button _discardButton;
    private readonly Button _cancelButton;
    private readonly Label _errorLabel;

    /// <summary>
    /// Raised when the user chooses to save a draft.
    /// </summary>
    public event Action? DraftSaved;

    /// <summary>
    /// Raised when the user chooses to discard and close.
    /// </summary>
    public event Action? Discarded;

    /// <summary>
    /// Raised when the user cancels the quit and returns to the form.
    /// </summary>
    public event Action? Cancelled;

    public QuitDialog(CommitFormViewModel formViewModel, DraftStore draftStore)
    {
        _formViewModel = formViewModel;
        _draftStore = draftStore;

        Title = "Quit?";

        Label message = new()
        {
            Text = "You have an in-progress commit. What would you like to do?",
            X = 0,
            Y = 0,
        };

        _errorLabel = new Label
        {
            Text = string.Empty,
            X = 0,
            Y = 2,
            Visible = false,
        };

        _saveDraftButton = new Button
        {
            Text = "_Save draft",
            X = Pos.Center() - 16,
            Y = 4,
            IsDefault = true,
        };
        _saveDraftButton.Accepting += async (_, _) => await OnSaveDraftAsync();

        _discardButton = new Button
        {
            Text = "_Discard",
            X = Pos.Center() - 2,
            Y = 4,
        };
        _discardButton.Accepting += (_, _) =>
        {
            Discarded?.Invoke();
        };

        _cancelButton = new Button
        {
            Text = "_Cancel",
            X = Pos.Center() + 10,
            Y = 4,
        };
        _cancelButton.Accepting += (_, _) =>
        {
            Cancelled?.Invoke();
        };

        Add(message, _errorLabel, _saveDraftButton, _discardButton, _cancelButton);
    }

    public IReadOnlyList<StatusBarKeyBinding> GetKeyBindings() =>
    [
        new(Key.Tab, "Tab next button"),
        new(Key.Enter, "Enter select"),
        new(Key.Esc, "Esc cancel"),
    ];

    private async Task OnSaveDraftAsync()
    {
        try
        {
            _errorLabel.Visible = false;
            await _draftStore.SaveDraftAsync(_formViewModel);
            DraftSaved?.Invoke();
        }
        catch (Exception ex)
        {
            _errorLabel.Text = $"Error saving draft: {ex.Message}";
            _errorLabel.Visible = true;
        }
    }
}
