/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Terminal.Gui.Drawing;

namespace CoAttribution.Cli.Tui.ViewModels;

/// <summary>
/// Backs the commit form with subject/body fields and a live N/72 subject counter.
/// </summary>
public sealed partial class CommitFormViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    public const int SubjectWarningThreshold = 50;
    public const int SubjectMaxThreshold = 72;

    public CommitFormViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// The commit message subject line (single-line).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SubjectLength))]
    [NotifyPropertyChangedFor(nameof(SubjectColor))]
    private string _subject = string.Empty;

    /// <summary>
    /// The commit message body (multi-line).
    /// </summary>
    [ObservableProperty]
    private string _body = string.Empty;

    /// <summary>
    /// Current character count of <see cref="Subject"/>.
    /// </summary>
    public int SubjectLength => Subject.Length;

    /// <summary>
    /// Color for the N/72 counter label based on subject length thresholds.
    /// </summary>
    public ColorName16 SubjectColor => SubjectLength switch
    {
        >= SubjectMaxThreshold => ColorName16.Red,
        >= SubjectWarningThreshold => ColorName16.BrightYellow,
        _ => ColorName16.Green
    };
}
