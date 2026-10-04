/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using Terminal.Gui.Drawing;
using Terminal.Gui.Editor;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace CoAttribution.Cli.Tui.Views;

/// <summary>
/// Composite sub-view that groups the commit-form subject and body controls into
/// labeled <see cref="FrameView"/> sections for a modern visual hierarchy (T019, T011, D008).
/// It exposes the inner controls so the parent <see cref="CommitFormView"/> can keep wiring
/// counters, hard caps, and Tab navigation unchanged (D001).
/// </summary>
public sealed class CommitFormSectionsView : View
{
    /// <summary>The labeled frame wrapping the subject field.</summary>
    public FrameView SubjectFrame { get; }

    /// <summary>The labeled frame wrapping the body editor.</summary>
    public FrameView BodyFrame { get; }

    /// <summary>The subject text field, exposed for focus-chain and counter wiring.</summary>
    public TextField SubjectField { get; }

    /// <summary>The body editor, exposed for counter and Tab-navigation wiring.</summary>
    public Editor BodyField { get; }

    /// <summary>The subject length counter label (e.g. "0/72").</summary>
    public Label SubjectCounterLabel { get; }

    /// <summary>The body length counter label (e.g. "0/1000").</summary>
    public Label BodyCounterLabel { get; }

    public CommitFormSectionsView()
    {
        Width = Dim.Fill();
        Height = Dim.Fill();

        // --- Subject section ---
        SubjectCounterLabel = new Label
        {
            Text = "0/72",
            X = Pos.AnchorEnd(8),
            Y = 0,
        };

        Label subjectLabel = new()
        {
            Text = "Subject:",
            X = 0,
            Y = 0,
        };

        SubjectField = new TextField
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = 1,
            CanFocus = true,
        };

        SubjectFrame = new FrameView
        {
            Title = "Subject",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 5,
        };
        SubjectFrame.Add(subjectLabel, SubjectCounterLabel, SubjectField);

        // --- Body section ---
        BodyCounterLabel = new Label
        {
            Text = "0/1000",
            X = Pos.AnchorEnd(8),
            Y = 0,
        };

        Label bodyLabel = new()
        {
            Text = "Body:",
            X = 0,
            Y = 0,
        };

        BodyField = new Editor
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            CanFocus = true,
            BorderStyle = LineStyle.Rounded,
            ViewportSettings = ViewportSettingsFlags.HasScrollBars,
        };

        BodyFrame = new FrameView
        {
            Title = "Body",
            X = 0,
            Y = Pos.Bottom(SubjectFrame),
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
        };
        BodyFrame.Add(bodyLabel, BodyCounterLabel, BodyField);

        Add(SubjectFrame, BodyFrame);
    }
}
