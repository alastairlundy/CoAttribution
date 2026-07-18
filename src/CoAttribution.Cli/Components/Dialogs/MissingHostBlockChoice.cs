/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Cli.Components.Dialogs;

/// <summary>
/// The three actions the user can take when a resolved host has no per-host
/// identity override block. Order matches the button order in the dialog
/// (left-to-right) so a future <c>default:</c> branch in a <c>switch</c> is stable.
/// </summary>
public enum MissingHostBlockChoice
{
    Add,
    SwitchHost,
    UseFallback
}
