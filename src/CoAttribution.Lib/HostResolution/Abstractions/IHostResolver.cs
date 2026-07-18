/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Lib.HostResolution.Abstractions;

/// <summary>
/// Resolves the current host from a 4-step precedence chain. The source-agnostic
/// <c>hostInput</c> parameter accepts an optional normalised host key from the
/// CLI flag, the TUI selector, a future <c>coattribution doctor</c> subcommand, or
/// a test fixture.
/// </summary>
public interface IHostResolver
{
    /// <param name="hostInput">
    /// Optional caller-supplied normalised host key that wins the precedence chain
    /// at the top of D003. May be null when the caller has no candidate to offer.
    /// </param>
    HostResolutionResult ResolveHost(string? hostInput);
}
