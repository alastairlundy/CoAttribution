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
using CoAttribution.Lib.Models.DTOs;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace CoAttribution.Cli.Tui.Dialogs;

/// <summary>
/// First-time and empty-registry guidance dialog. Walks a user who has just
/// installed CoAttribution (or whose registry is empty) through adding their
/// first author before the main commit flow is reached.
/// </summary>
public sealed class SetupDialog : Window, IStatusBarProvider
{
    private readonly IAuthorRegistry _authorRegistry;
    private readonly TextField _nameField;
    private readonly TextField _emailField;
    private readonly OptionSelector _attributionRadio;
    private readonly Button _addButton;
    private readonly Button _cancelButton;
    private readonly Label _errorLabel;

    /// <summary>
    /// Raised when the user successfully adds an author and the dialog completes.
    /// </summary>
    public event Action? AuthorAdded;

    /// <summary>
    /// Raised when the user cancels the dialog.
    /// </summary>
    public event Action? Cancelled;

    public SetupDialog(IAuthorRegistry authorRegistry)
    {
        _authorRegistry = authorRegistry;

        Title = "Setup — Add Your First Author";
        Padding.Thickness = new Thickness(2);
        BorderStyle = LineStyle.Rounded;

        // --- Name field ---
        Label nameLabel = new()
        {
            Text = "Name:",
            X = Pos.Center() - 15,
            Y = 2,
        };

        _nameField = new TextField
        {
            X = Pos.Center() - 10,
            Y = 2,
            Width = Dim.Fill(2),
        };

        // --- Email field ---
        Label emailLabel = new()
        {
            Text = "Email:",
            X = Pos.Center() - 15,
            Y = 4,
        };

        _emailField = new TextField
        {
            X = Pos.Center() - 10,
            Y = 4,
            Width = Dim.Fill(2),
        };

        // --- Default attribution type selector ---
        Label attrLabel = new()
        {
            Text = "Default attribution:",
            X = Pos.Center() - 15,
            Y = 6,
        };

        _attributionRadio = new OptionSelector
        {
            X = Pos.Center() - 15,
            Y = 7,
            Labels = ["Co-author", "Assisted-by", "Default"],
        };

        // --- Error label (hidden by default) ---
        _errorLabel = new Label
        {
            Text = string.Empty,
            X = Pos.Center() - 15,
            Y = 10,
            Visible = false,
        };

        // --- Separator ---
        Line separator = new()
        {
            X = Pos.Center() - 15,
            Y = 11,
            Width = Dim.Fill(2),
        };

        // --- Buttons ---
        _addButton = new Button
        {
            Text = "_Add author",
            X = Pos.Center() - 15,
            Y = 12,
            IsDefault = true,
            Width = 14,
        };
        _addButton.Accepting += async (_, _) => await OnAddAsync();

        _cancelButton = new Button
        {
            Text = "_Cancel",
            X = Pos.Center() + 2,
            Y = 12,
            Width = 14,
        };
        _cancelButton.Accepting += (_, _) =>
        {
            Cancelled?.Invoke();
        };

        Add(nameLabel, _nameField,
            emailLabel, _emailField,
            attrLabel, _attributionRadio,
            _errorLabel,
            separator,
            _addButton, _cancelButton);
    }

    public IReadOnlyList<StatusBarKeyBinding> GetKeyBindings() =>
    [
        new(Key.Tab, "Tab next field"),
        new(Key.Enter, "Enter confirm"),
        new(Key.Esc, "Esc cancel"),
    ];

    private async Task OnAddAsync()
    {
        string name = _nameField.Text?.ToString()?.Trim() ?? string.Empty;
        string email = _emailField.Text?.ToString()?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
        {
            _errorLabel.Text = "Name and Email are required.";
            _errorLabel.Visible = true;
            return;
        }

        AttributionType defaultType = DetermineSelectedAttributionType();

        GitCoAuthor author = new()
        {
            CoAuthorId = GenerateCoAuthorId(name),
            Name = name,
            Email = email,
            DefaultAttributionType = defaultType,
            Type = ContributorType.Human,
        };

        try
        {
            await _authorRegistry.AddAsync(author, CancellationToken.None);
            _errorLabel.Visible = false;
            AuthorAdded?.Invoke();
        }
        catch (Exception ex)
        {
            _errorLabel.Text = $"Error: {ex.Message}";
            _errorLabel.Visible = true;
        }
    }

    private AttributionType DetermineSelectedAttributionType()
    {
        return _attributionRadio.Value switch
        {
            1 => AttributionType.Assisted,
            2 => AttributionType.DefaultOrCoAuthor,
            _ => AttributionType.CoAuthor,
        };
    }

    /// <summary>
    /// Generates a CoAuthorId from the name by lowercasing and replacing spaces with hyphens.
    /// </summary>
    private static string GenerateCoAuthorId(string name)
    {
        return name.ToLowerInvariant()
            .Replace(' ', '-')
            .Replace('_', '-');
    }
}
