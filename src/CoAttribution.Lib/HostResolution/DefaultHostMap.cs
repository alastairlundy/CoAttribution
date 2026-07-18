/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Lib.HostResolution;

public static class DefaultHostMap
{
    public static readonly IReadOnlyDictionary<string, string> Entries =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["github.com"] = "github",
            ["gitlab.com"] = "gitlab",
            ["bitbucket.org"] = "bitbucket",
            ["gitea.com"] = "gitea",
            ["codeberg.org"] = "codeberg",
        };
}
