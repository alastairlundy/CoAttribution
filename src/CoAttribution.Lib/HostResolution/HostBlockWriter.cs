/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Lib.Models;
using CoAttribution.Lib.Models.DTOs;

namespace CoAttribution.Lib.HostResolution;

// ReSharper disable once PartialTypeWithSinglePart
public partial class HostBlockWriter
{
    public GitCoAuthorConfig Write(
        GitCoAuthorConfig config,
        string contributorId,
        string hostKey,
        HostOverride block)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(contributorId);
        ArgumentNullException.ThrowIfNull(hostKey);
        ArgumentNullException.ThrowIfNull(block);

        if (!HostKeyValidator.IsValid(hostKey))
        {
            throw new ArgumentException(
                $"Host key '{hostKey}' is invalid. The caller must validate host keys via HostKeyValidator before calling Write.",
                nameof(hostKey));
        }

        GitCoAuthor? contributor = null;

        if (config.Agents.TryGetValue(contributorId, out GitCoAuthor? agent))
        {
            contributor = agent;
        }
        else if (config.Humans.TryGetValue(contributorId, out GitCoAuthor? human))
        {
            contributor = human;
        }

        if (contributor is null)
        {
            throw new KeyNotFoundException(
                $"Contributor '{contributorId}' was not found in config.Agents or config.Humans.");
        }

        contributor.Host[hostKey] = block;

        return config;
    }
}
