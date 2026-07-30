/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Lib.HostResolution;

namespace CoAttribution.Lib.Exceptions;

public sealed class MissingHostBlockException : Exception
{
    public IReadOnlyList<MissingHostBlockDiagnostic> Diagnostics { get; }

    public MissingHostBlockException(IReadOnlyList<MissingHostBlockDiagnostic> diagnostics)
        : base($"One or more per-host identity blocks are missing for the resolved host.")
    {
        Diagnostics = diagnostics;
    }
}
