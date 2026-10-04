/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace CoAttribution.Cli.Tui.Views;

/// <summary>
/// The kind of commit feedback being shown in the <see cref="FeedbackToast"/>.
/// </summary>
public enum FeedbackKind
{
    /// <summary>The commit succeeded.</summary>
    Success,

    /// <summary>The commit failed (non-zero exit).</summary>
    Failure,

    /// <summary>An exception occurred while building or executing the commit.</summary>
    Error,
}

/// <summary>
/// A transient, non-blocking overlay used to communicate commit outcome.
/// Replaces the old practice of mutating <c>Window.Title</c> so the title stays a
/// stable identity string (D003, T009, T017). Auto-dismisses via a single
/// <see cref="MainLoop"/> timeout (AoT-safe timer lifecycle, ADR 0001).
/// </summary>
public sealed class FeedbackToast : View
{
    private readonly FrameView _frame;
    private readonly Label _label;

    // Monotonic token that invalidates any pending auto-dismiss timer when a new
    // Show/Dismiss happens, so we never need to hold the timeout handle directly.
    private int _dismissToken;

    public FeedbackToast()
    {
        _label = new Label
        {
            X = 1,
            Y = 0,
            Width = Dim.Fill(1),
            Height = 1,
        };

        _frame = new FrameView
        {
            X = Pos.Center(),
            Y = Pos.AnchorEnd(3),
            Width = 20,
            Height = 3,
            CanFocus = false,
        };
        _frame.Add(_label);

        Add(_frame);

        Visible = false;
        CanFocus = false;
    }

    /// <summary>
    /// Shows the toast with <paramref name="message"/> and schedules an auto-dismiss.
    /// </summary>
    public void Show(string message, FeedbackKind kind)
    {
        _label.Text = FormatMessage(message, kind);

        _frame.Width = Math.Max(message.Length + 4, 20);

        Visible = true;

        // Bring the toast to the top of its SuperView's draw order so it renders
        // above the active screen, without depending on insertion order.
        SuperView?.MoveSubViewToEnd(this);

        SetNeedsDraw();

        int myToken = ++_dismissToken;
        App?.TimedEvents?.Add(TimeSpan.FromSeconds(2.5), () =>
        {
            if (_dismissToken == myToken)
            {
                Dismiss();
            }

            return false;
        });
    }

    /// <summary>
    /// Immediately hides the toast and cancels any pending auto-dismiss.
    /// </summary>
    public void Dismiss()
    {
        _dismissToken++;
        Visible = false;
        SetNeedsDraw();
    }

    private static string FormatMessage(string message, FeedbackKind kind) => kind switch
    {
        FeedbackKind.Success => $"✔ {message}",
        FeedbackKind.Failure => $"⚠ {message}",
        _ => $"⚠ {message}",
    };
}
