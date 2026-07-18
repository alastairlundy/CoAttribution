/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Lib.HostResolution;

/// <summary>
/// Self-contained, human-readable explanation of a missing per-host identity block.
/// Consumed by the CLI diagnostic formatter to render a localized message.
/// </summary>
public sealed record MissingHostBlockDiagnostic(
    string HostKey,
    string ContributorId,
    string RegistryPath,
    string TomlSnippet);
