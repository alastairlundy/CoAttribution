/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Diagnostics.CodeAnalysis;

namespace CoAttribution.Lib.HostResolution.Abstractions;

/// <summary>
/// Read/write seam for <c>.git/config</c> keys under the <c>coattribution.*</c> namespace.
/// </summary>
public interface IGitConfigClient
{
    /// <summary>
    /// Reads a single <c>.git/config</c> value. Returns <c>true</c> when the key is set.
    /// </summary>
    bool TryGet(string key, [NotNullWhen(true)] out string? value);

    /// <summary>
    /// Writes a <c>.git/config</c> value. Only <c>coattribution.*</c> keys are accepted;
    /// any other key throws <see cref="ArgumentException"/>.
    /// </summary>
    void Set(string key, string value);
}
