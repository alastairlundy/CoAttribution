/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Lib.HostResolution;

/// <summary>
/// Result of a host resolution attempt. Carries one of three variants (Resolved,
/// MissingBlock, NoHostDetected) along with optional payload data depending on the variant.
/// </summary>
public readonly record struct HostResolutionResult
{
    /// <summary>
    /// Which outcome variant this result represents.
    /// </summary>
    public HostResolutionVariant Variant { get; init; }

    /// <summary>
    /// The per-host identity override block, populated for the Resolved variant.
    /// </summary>
    public Models.DTOs.HostOverride? Override { get; init; }

    /// <summary>
    /// The validated host key, populated for the Resolved and MissingBlock variants.
    /// </summary>
    public string? HostKey { get; init; }

    /// <summary>
    /// The source step of the precedence chain that produced the host, populated for the Resolved variant.
    /// </summary>
    public HostSource Source { get; init; }

    /// <summary>
    /// The contributor id whose override block is missing, populated for the MissingBlock variant.
    /// </summary>
    public string? ContributorId { get; init; }
}
