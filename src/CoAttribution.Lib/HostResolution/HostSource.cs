/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Lib.HostResolution;

/// <summary>
/// Identifies which step of the 4-step host precedence chain (D003) supplied the resolved host.
/// </summary>
public enum HostSource
{
    CliFlag,
    GitConfig,
    RemoteProbe,
    Fallback
}
