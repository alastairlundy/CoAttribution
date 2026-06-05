/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Lib.Extensions;

public static class CoAuthorConfigExtensions
{
    extension(GitCoAuthorConfig config)
    {
        public IEnumerable<GitCoAuthor> EnumerateCoAuthors()
            => config.Agents.Values.Concat(config.Humans.Values);
        
        public GitCoAuthor[] GetCoAuthors()
            => config.EnumerateCoAuthors().ToArray();
    }
}