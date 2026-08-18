/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Cli.Tui.ViewModels;

/// <summary>
/// Serializable snapshot of in-progress commit form state.
/// Persisted by <see cref="DraftStore"/> so the TUI can resume
/// on the next launch after an accidental quit.
/// </summary>
public sealed record DraftState(string Subject, string Body);
