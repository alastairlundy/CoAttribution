/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Lib.Abstractions;

public interface IRegistryPathResolver
{
    Task<string?> GetGlobalRegistryPathAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns an explicitly requested registry path (e.g. from a <c>--registry</c>
    /// CLI option), or <c>null</c> when no explicit override was supplied.
    /// </summary>
    Task<string?> GetExplicitRegistryPathAsync(CancellationToken cancellationToken);
}
