/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Cli.Tui.Abstractions;
using CoAttribution.Cli.Tui.Composition;
using CoAttribution.Cli.Tui.ViewModels;
using CoAttribution.Lib.Abstractions;
using Terminal.Gui.Drawing;
using Terminal.Gui.Editor;
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
    private readonly IRepositoryContext _repositoryContext;
    private readonly GlyphSet _glyphSet;
    private readonly CommitFormSectionsView _sections;
    private bool _suppressSubjectChange;
    private bool _suppressBodyChange;

    /// <summary>
    /// The subject text field, exposed for focus-chain checks from MainWindow.
    /// </summary>
    public TextField SubjectField => _sections.SubjectField;

    public CommitFormView(CommitFormViewModel viewModel, IRepositoryContext repositoryContext, GlyphSet glyphSet)
    {
        _viewModel = viewModel;
        _repositoryContext = repositoryContext;
        _glyphSet = glyphSet;

        Title = "Commit Message";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;
        Padding.Thickness = new Thickness(1, 0, 1, 0);

        // --- Repository context header ---
        Label repoLabel = new()
        {
            Text = GetRepoContextLabel(),
            X = 0,
            Y = 0,
        };

        // --- Subject/Body sections (counters, caps, and Tab nav wired below) ---
        _sections = new CommitFormSectionsView
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
        };

        // Block characters beyond the hard cap
        _sections.SubjectField.KeyDown += (_, e) =>
        {
            if (_viewModel.SubjectLength >= CommitFormViewModel.SubjectMaxThreshold
                && !IsNavigationKey(e))
            {
                e.Handled = true;
            }
        };

        _sections.SubjectField.TextChanged += (_, _) =>
        {
            if (_suppressSubjectChange)
                return;

            string text = _sections.SubjectField.Text?.ToString() ?? string.Empty;

            // Truncate if paste exceeded the cap
            if (text.Length > CommitFormViewModel.SubjectMaxThreshold)
            {
                _suppressSubjectChange = true;
                _sections.SubjectField.Text = text[..CommitFormViewModel.SubjectMaxThreshold];
                _suppressSubjectChange = false;
                text = _sections.SubjectField.Text?.ToString() ?? string.Empty;
            }

            _viewModel.Subject = text;
            _sections.SubjectCounterLabel.Text = FormatSubjectCounter(_viewModel.SubjectLength);
            UpdateSubjectCounterColor();
        };

        // Tab moves focus back to subject (Editor uses Tab for indentation by default)
        _sections.BodyField.KeyDown += (_, e) =>
        {
            if (e == Key.Tab)
            {
                _sections.SubjectField.SetFocus();
                // Terminal.Gui v2 TextField auto-selects all text on keyboard focus;
                // move the caret to the end so the user can keep typing.
                _sections.SubjectField.InsertionPoint = _sections.SubjectField.Text?.Length ?? 0;
                e.Handled = true;
            }
        };

        _sections.BodyField.ContentChanged += (_, _) =>
        {
            if (_suppressBodyChange)
                return;

            string text = _sections.BodyField.Text ?? string.Empty;

            // Truncate if content exceeded the hard cap
            if (text.Length > CommitFormViewModel.BodyHardThreshold)
            {
                _suppressBodyChange = true;
                _sections.BodyField.Text = text[..CommitFormViewModel.BodyHardThreshold];
                _suppressBodyChange = false;
                text = _sections.BodyField.Text ?? string.Empty;
            }

            _viewModel.Body = text;
            _sections.BodyCounterLabel.Text = FormatBodyCounter(_viewModel.BodyLength);
            UpdateBodyCounterColor();
        };

        Add(repoLabel, _sections);
    }

    public void Initialize()
    {
        _viewModel.Subject = string.Empty;
        _viewModel.Body = string.Empty;
    }

    /// <summary>
    /// Builds the repo context label text: "owner/repo @ branch".
    /// </summary>
    private string GetRepoContextLabel()
    {
        string repoName = _repositoryContext.GetRepositoryNameAsync().GetAwaiter().GetResult();
        string branch = _repositoryContext.GetCurrentBranch();
        return $"{repoName} @ {branch}";
    }

    public void FocusSubject()
    {
        _sections.SubjectField.SetFocus();
    }

    public IReadOnlyList<StatusBarKeyBinding> GetKeyBindings() =>
    [
        new(Key.Enter, "Enter next", _glyphSet.KeyEnter),
        new(Key.Tab, "Tab next field", _glyphSet.KeyTab),
        new(Key.Esc, "Esc quit", _glyphSet.KeyEsc),
    ];

    private static string FormatSubjectCounter(int length) => $"{length}/72";

    private static string FormatBodyCounter(int length) => $"{length}/1000";

    private void UpdateSubjectCounterColor()
    {
        _sections.SubjectCounterLabel.SetScheme(new Scheme
        {
            Normal = new Terminal.Gui.Drawing.Attribute(_viewModel.SubjectColor, ColorName16.Black),
        });
    }

    private void UpdateBodyCounterColor()
    {
        _sections.BodyCounterLabel.SetScheme(new Scheme
        {
            Normal = new Terminal.Gui.Drawing.Attribute(_viewModel.BodyColor, ColorName16.Black),
        });
    }

    private static bool IsNavigationKey(Key e)
    {
        return e == Key.Backspace || e == Key.Delete
            || e == Key.CursorLeft || e == Key.CursorRight
            || e == Key.Home || e == Key.End
            || e == Key.C.WithCtrl || e == Key.V.WithCtrl
            || e == Key.X.WithCtrl || e == Key.A.WithCtrl;
    }
}
