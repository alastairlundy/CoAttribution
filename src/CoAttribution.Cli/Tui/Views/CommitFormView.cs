/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Cli.Tui.Abstractions;
using CoAttribution.Cli.Tui.ViewModels;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace CoAttribution.Cli.Tui.Views;

/// <summary>
/// Commit-message editor with a single-line subject, multi-line body,
/// and a live N/72 subject counter (warning at 50, red at 72).
/// </summary>
public sealed class CommitFormView : View, IStatusBarProvider
{
    private readonly CommitFormViewModel _viewModel;
    private readonly Label _counterLabel;

    public CommitFormView(CommitFormViewModel viewModel)
    {
        _viewModel = viewModel;

        Title = "Commit Message";

        // Subject label
        Label subjectLabel = new()
        {
            Text = "Subject:",
            X = 0,
            Y = 0,
        };

        // N/72 counter — right-aligned next to the subject field
        _counterLabel = new Label
        {
            Text = FormatCounter(0),
            X = Pos.AnchorEnd(6),
            Y = 0,
        };
        UpdateCounterColor();

        // Subject field (single-line)
        TextField subjectField = new()
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
        };
        subjectField.TextChanged += (_, _) =>
        {
            _viewModel.Subject = subjectField.Text?.ToString() ?? string.Empty;
            _counterLabel.Text = FormatCounter(_viewModel.SubjectLength);
            UpdateCounterColor();
        };

        // Body label
        Label bodyLabel = new()
        {
            Text = "Body:",
            X = 0,
            Y = 3,
        };

        // Body field (multi-line)
#pragma warning disable CS0618 // TextView is superseded by EditorView but is the spec'd control for this ticket
        TextView bodyField = new()
#pragma warning restore CS0618
        {
            X = 0,
            Y = 4,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        bodyField.TextChanged += (_, _) =>
        {
            _viewModel.Body = bodyField.Text?.ToString() ?? string.Empty;
        };

        Add(subjectLabel, _counterLabel, subjectField, bodyLabel, bodyField);
    }

    /// <summary>
    /// Resets the form to a clean state for a new commit.
    /// </summary>
    public void Initialize()
    {
        _viewModel.Subject = string.Empty;
        _viewModel.Body = string.Empty;
    }

    public IReadOnlyList<StatusBarKeyBinding> GetKeyBindings() =>
    [
        new(Key.Enter, "Enter next"),
        new(Key.Tab, "Tab next field"),
        new(Key.Esc, "Esc quit"),
    ];

    private static string FormatCounter(int length) => $"{length}/72";

    private void UpdateCounterColor()
    {
        _counterLabel.SetScheme(new Scheme
        {
            Normal = new Terminal.Gui.Drawing.Attribute(_viewModel.SubjectColor, ColorName16.Black),
        });
    }
}
