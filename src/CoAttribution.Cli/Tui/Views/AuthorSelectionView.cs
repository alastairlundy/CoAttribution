/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Cli.Tui.Abstractions;
using CoAttribution.Cli.Tui.ViewModels;
using CoAttribution.Lib.Models;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace CoAttribution.Cli.Tui.Views;

/// <summary>
/// Multi-select author checklist: every registered author plus a resolved host
/// row appears as a toggleable row, with a type-ahead filter, AI/bot visual
/// distinction, a basic/advanced view toggle, and an in-place "Add author" action.
/// </summary>
public sealed class AuthorSelectionView : View, IStatusBarProvider
{
    private readonly AuthorSelectionViewModel _viewModel;
    private readonly TextField _filterField;
    private readonly CheckBox _advancedToggle;
    private readonly View _authorListContainer;
    private readonly Button _addAuthorButton;
    private readonly Label _errorLabel;

    /// <summary>
    /// Raised when the user confirms their author selection.
    /// Carries the selected author IDs grouped by attribution type.
    /// </summary>
    public event Action<string[], string[], string[]>? Confirmed;

    /// <summary>
    /// Raised when the user requests adding a new author (opens AddAuthorDialog).
    /// </summary>
    public event Action? AddAuthorRequested;

    /// <summary>
    /// Raised when host resolution fails with a missing host block.
    /// Carries the contributor ID and host key needed to create the block.
    /// </summary>
    public event Action<string, string>? HostBlockMissing;

    public AuthorSelectionView(AuthorSelectionViewModel viewModel)
    {
        _viewModel = viewModel;

        Title = "Select Authors";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;

        // --- Type-ahead filter ---
        Label filterLabel = new()
        {
            Text = "Filter:",
            X = 0,
            Y = 0,
        };

        _filterField = new TextField
        {
            X = 8,
            Y = 0,
            Width = Dim.Fill(),
        };
        _filterField.TextChanged += (_, _) =>
        {
            _viewModel.FilterText = _filterField.Text?.ToString() ?? string.Empty;
            RebuildCheckboxes();
        };

        // --- Advanced view toggle ---
        _advancedToggle = new CheckBox
        {
            Text = "Advanced view",
            X = 0,
            Y = 2,
        };
        _advancedToggle.ValueChanged += (_, args) =>
        {
            _viewModel.AdvancedViewEnabled = args.NewValue == CheckState.Checked;
            RebuildCheckboxes();
        };

        // --- Error label (hidden by default) ---
        _errorLabel = new Label
        {
            Text = string.Empty,
            X = 0,
            Y = 3,
            Visible = false,
        };

        // --- Author list ---
        _authorListContainer = new View
        {
            X = 0,
            Y = 4,
            Width = Dim.Fill(),
            Height = Dim.Fill(1), // leave room for the Add button
        };

        // --- Add author button ---
        _addAuthorButton = new Button
        {
            Text = "+ Add author",
            X = Pos.Center(),
            Y = Pos.AnchorEnd(0),
        };
        _addAuthorButton.Accepting += (_, _) =>
        {
            AddAuthorRequested?.Invoke();
        };

        Add(filterLabel, _filterField, _advancedToggle, _errorLabel, _authorListContainer, _addAuthorButton);

        // Raise Confirmed on Enter with current selection
        KeyDown += (_, e) =>
        {
            if (e == Key.Enter)
            {
                var (coAuthorIds, assistIds, defaultIds) = _viewModel.GetSelectedIds();
                Confirmed?.Invoke(coAuthorIds, assistIds, defaultIds);
                e.Handled = true;
            }
        };
    }

    /// <summary>
    /// Loads data from the view model and builds the checkbox list.
    /// Call this before showing the view (or after AddAuthorDialog returns).
    /// </summary>
    public async Task LoadAsync()
    {
        await _viewModel.LoadAsync();
        SyncErrorState();
        RebuildCheckboxes();
    }

    /// <summary>
    /// Preserves current selections, filter text, and toggle state across a round-trip
    /// (e.g. after returning from AddAuthorDialog). Reloads from registry and re-applies state.
    /// </summary>
    public async Task RefreshAsync()
    {
        string savedFilter = _viewModel.FilterText;
        bool savedAdvanced = _viewModel.AdvancedViewEnabled;

        await _viewModel.LoadAsync();

        _viewModel.FilterText = savedFilter;
        _filterField.Text = savedFilter;
        _viewModel.AdvancedViewEnabled = savedAdvanced;
        _advancedToggle.Value = savedAdvanced ? CheckState.Checked : CheckState.UnChecked;

        SyncErrorState();
        RebuildCheckboxes();
    }

    public IReadOnlyList<StatusBarKeyBinding> GetKeyBindings() =>
    [
        new(Key.Space, "Space toggle"),
        new(Key.Enter, "Enter confirm"),
        new(Key.Esc, "Esc quit"),
    ];

    /// <summary>
    /// Rebuilds the checkbox list from the current filtered rows in the view model.
    /// </summary>
    private void RebuildCheckboxes()
    {
        _authorListContainer.RemoveAll();

        IReadOnlyList<AuthorRow> rows = _viewModel.Rows;
        int y = 0;

        foreach (AuthorRow row in rows)
        {
            CheckBox cb = new()
            {
                Text = row.DisplayLabel,
                X = 0,
                Y = y,
                Value = row.IsSelected ? CheckState.Checked : CheckState.UnChecked,
            };

            // Capture row in closure
            AuthorRow capturedRow = row;
            cb.ValueChanged += (_, args) =>
            {
                capturedRow.IsSelected = args.NewValue == CheckState.Checked;
            };

            // In advanced view, show attribution selector after each checkbox
            if (_viewModel.AdvancedViewEnabled && !capturedRow.IsHostRow)
            {
                Button attrButton = new()
                {
                    Text = FormatAttributionType(capturedRow.SelectedAttributionType),
                    X = Pos.Right(cb) + 1,
                    Y = y,
                    Width = 16,
                };

                Button capturedAttrButton = attrButton;
                attrButton.Accepting += (_, _) =>
                {
                    // Cycle through: CoAuthor → Assisted → DefaultOrCoAuthor → CoAuthor
                    capturedRow.SelectedAttributionType = capturedRow.SelectedAttributionType switch
                    {
                        AttributionType.CoAuthor => AttributionType.Assisted,
                        AttributionType.Assisted => AttributionType.DefaultOrCoAuthor,
                        _ => AttributionType.CoAuthor,
                    };
                    capturedAttrButton.Text = FormatAttributionType(capturedRow.SelectedAttributionType);
                };

                _authorListContainer.Add(cb, attrButton);
            }
            else
            {
                _authorListContainer.Add(cb);
            }

            y++;
        }
    }

    private void SyncErrorState()
    {
        if (_viewModel.HasHostError)
        {
            _errorLabel.Text = $"⚠ Host resolution: {_viewModel.HostErrorMessage}";
            _errorLabel.Visible = true;

            // Notify MainWindow so it can open MissingHostBlockDialog
            if (_viewModel.MissingHostContributorId is not null && _viewModel.MissingHostKey is not null)
            {
                HostBlockMissing?.Invoke(_viewModel.MissingHostContributorId, _viewModel.MissingHostKey);
            }
        }
        else
        {
            _errorLabel.Text = string.Empty;
            _errorLabel.Visible = false;
        }
    }

    private static string FormatAttributionType(AttributionType type) => type switch
    {
        AttributionType.CoAuthor => "Co-author",
        AttributionType.Assisted => "Assisted-by",
        _ => "Default",
    };
}
