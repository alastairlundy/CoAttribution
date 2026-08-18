/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Cli.Tui.Abstractions;
using CoAttribution.Lib.Abstractions;
using CoAttribution.Lib.Models;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace CoAttribution.Cli.Tui.Dialogs;

/// <summary>
/// Modal dialog that lets a user add a new author to the registry mid-commit
/// without leaving the author-selection screen. Captures (name, email), writes
/// through <see cref="IAuthorRegistry.AddAsync"/>, and signals the caller to
/// refresh.
/// </summary>
public sealed class AddAuthorDialog : Window, IStatusBarProvider
{
    private readonly IAuthorRegistry _authorRegistry;
    private readonly TextField _nameField;
    private readonly TextField _emailField;
    private readonly Button _addButton;
    private readonly Button _cancelButton;
    private readonly Label _errorLabel;

    /// <summary>
    /// Raised when an author has been successfully added to the registry.
    /// The caller should refresh its author list.
    /// </summary>
    public event Action? AuthorAdded;

    /// <summary>
    /// Raised when the user cancels the dialog.
    /// </summary>
    public event Action? Cancelled;

    public AddAuthorDialog(IAuthorRegistry authorRegistry)
    {
        _authorRegistry = authorRegistry;

        Title = "Add Author";

        Label nameLabel = new()
        {
            Text = "Name:",
            X = 0,
            Y = 0,
        };

        _nameField = new TextField
        {
            X = 10,
            Y = 0,
            Width = Dim.Fill(),
        };
        _nameField.TextChanged += (_, _) => UpdateAddButtonState();

        Label emailLabel = new()
        {
            Text = "Email:",
            X = 0,
            Y = 2,
        };

        _emailField = new TextField
        {
            X = 10,
            Y = 2,
            Width = Dim.Fill(),
        };
        _emailField.TextChanged += (_, _) => UpdateAddButtonState();

        _errorLabel = new Label
        {
            Text = string.Empty,
            X = 0,
            Y = 4,
            Visible = false,
        };

        _addButton = new Button
        {
            Text = "_Add author",
            X = Pos.Center() - 10,
            Y = 6,
            IsDefault = true,
            Enabled = false,
        };
        _addButton.Accepting += async (_, _) => await OnAddAsync();

        _cancelButton = new Button
        {
            Text = "_Cancel",
            X = Pos.Center() + 2,
            Y = 6,
        };
        _cancelButton.Accepting += (_, _) =>
        {
            Cancelled?.Invoke();
        };

        Add(nameLabel, _nameField, emailLabel, _emailField, _errorLabel, _addButton, _cancelButton);
    }

    public IReadOnlyList<StatusBarKeyBinding> GetKeyBindings() =>
    [
        new(Key.Tab, "Tab next field"),
        new(Key.Enter, "Enter add"),
        new(Key.Esc, "Esc cancel"),
    ];

    private void UpdateAddButtonState()
    {
        string name = _nameField.Text?.ToString()?.Trim() ?? string.Empty;
        string email = _emailField.Text?.ToString()?.Trim() ?? string.Empty;

        _addButton.Enabled = !string.IsNullOrWhiteSpace(name) && IsValidEmail(email);
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        int atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return false;

        int dotIndex = email.LastIndexOf('.');
        return dotIndex > atIndex + 1 && dotIndex < email.Length - 1;
    }

    private async Task OnAddAsync()
    {
        string name = _nameField.Text?.ToString()?.Trim() ?? string.Empty;
        string email = _emailField.Text?.ToString()?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name) || !IsValidEmail(email))
        {
            _errorLabel.Text = "Please enter a valid name and email.";
            _errorLabel.Visible = true;
            return;
        }

        try
        {
            _errorLabel.Visible = false;

            GitCoAuthor newAuthor = new()
            {
                Name = name,
                Email = email,
                Type = ContributorType.Human,
                DefaultAttributionType = AttributionType.DefaultOrCoAuthor,
            };

            await _authorRegistry.AddAsync(newAuthor, CancellationToken.None);

            AuthorAdded?.Invoke();
        }
        catch (Exception ex)
        {
            _errorLabel.Text = $"Error: {ex.Message}";
            _errorLabel.Visible = true;
        }
    }
}
