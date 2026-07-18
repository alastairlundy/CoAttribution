/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Lib.HostResolution.Abstractions;

/// <summary>
/// Probes the git working tree for the primary remote URL.
/// </summary>
public interface IGitRemoteProbe
{
    /// <summary>
    /// Returns the primary remote URL, preferring <c>origin</c> and falling back to the
    /// first configured remote. Returns <c>null</c> when no remotes are configured or
    /// the output is unparseable.
    /// </summary>
    Task<string?> GetPrimaryRemoteUrlAsync(CancellationToken cancellationToken = default);
}
