/*
    CoAuthorLib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAuthorLib.Extensions;

public static class CoAuthorModelExtensions
{
    extension(IEnumerable<GitCoAuthor> coAuthors)
    {
        public IEnumerable<GitCoAuthor> EnumerateHumanCoAuthors()
            => coAuthors.Where(c => c.Type == ContributorType.Human);
        
        public IEnumerable<GitCoAuthor> EnumerateAgentCoAuthors()
            => coAuthors.Where(c => c.Type == ContributorType.Agent);
    }
    
    extension(GitCoAuthor[] coAuthors)
    {
        public GitCoAuthor[] GetHumanCoAuthors()
            => coAuthors.Where(c => c.Type == ContributorType.Human)
                .ToArray();
        
        public GitCoAuthor[] GetAgentCoAuthors()
            => coAuthors.Where(c => c.Type == ContributorType.Agent)
                .ToArray();
    }
}