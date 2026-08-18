/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Cli.Tui.Abstractions;
using CoAttribution.Lib.Abstractions;
using CoAttribution.Lib.HostResolution;
using CoAttribution.Lib.Models.DTOs;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace CoAttribution.Cli.Tui.Dialogs;

/// <summary>
/// Recovery dialog shown when host resolution returns MissingBlock.
/// Captures the user's (name, email) for the missing host, writes it
/// through <see cref="HostBlockWriter"/>, and signals the caller to
/// retry the commit flow — all without leaving the TUI.
/// </summary>
public sealed class MissingHostBlockDialog : Window, IStatusBarProvider
{
    private readonly IAuthorRegistry _authorRegistry;
    private readonly HostBlockWriter _hostBlockWriter;
    private readonly string _contributorId;
    private readonly string _hostKey;
    private readonly TextField _nameField;
    private readonly TextField _emailField;
    private readonly Button _saveButton;
    private readonly Button _cancelButton;
    private readonly Label _errorLabel;

    /// <summary>
    /// Raised when the host block has been successfully written to the registry.
    /// The caller should re-resolve the host and refresh the author selection.
    /// </summary>
    public event Action? HostBlockWritten;

    /// <summary>
    /// Raised when the user cancels the dialog.
    /// </summary>
    public event Action? Cancelled;

    public MissingHostBlockDialog(
        IAuthorRegistry authorRegistry,
        HostBlockWriter hostBlockWriter,
        string contributorId,
        string hostKey)
    {
        _authorRegistry = authorRegistry;
        _hostBlockWriter = hostBlockWriter;
        _contributorId = contributorId;
        _hostKey = hostKey;

        Title = "Add Host Identity";

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
        _nameField.TextChanged += (_, _) => UpdateSaveButtonState();

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
        _emailField.TextChanged += (_, _) => UpdateSaveButtonState();

        _errorLabel = new Label
        {
            Text = string.Empty,
            X = 0,
            Y = 4,
            Visible = false,
        };

        _saveButton = new Button
        {
            Text = "_Save",
            X = Pos.Center() - 10,
            Y = 6,
            IsDefault = true,
            Enabled = false,
        };
        _saveButton.Accepting += async (_, _) => await OnSaveAsync();

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

        Add(nameLabel, _nameField, emailLabel, _emailField, _errorLabel, _saveButton, _cancelButton);
    }

    public IReadOnlyList<StatusBarKeyBinding> GetKeyBindings() =>
    [
        new(Key.Tab, "Tab next field"),
        new(Key.Enter, "Enter save"),
        new(Key.Esc, "Esc cancel"),
    ];

    private void UpdateSaveButtonState()
    {
        string name = _nameField.Text?.ToString()?.Trim() ?? string.Empty;
        string email = _emailField.Text?.ToString()?.Trim() ?? string.Empty;

        _saveButton.Enabled = !string.IsNullOrWhiteSpace(name) && IsValidEmail(email);
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

    private async Task OnSaveAsync()
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

            GitCoAuthorConfig config = await _authorRegistry.GetAuthorConfigAsync(CancellationToken.None);

            _hostBlockWriter.Write(config, _contributorId, _hostKey, new HostOverride
            {
                Name = name,
                Email = email,
            });

            await _authorRegistry.SaveConfigAsync(config, CancellationToken.None);

            HostBlockWritten?.Invoke();
        }
        catch (Exception ex)
        {
            _errorLabel.Text = $"Error: {ex.Message}";
            _errorLabel.Visible = true;
        }
    }
}
