/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Lib.Builders;

public interface ICommitMessageBuilder
{
    ICommitMessageBuilder SetContent(string subject, string body);

    ICommitMessageBuilder AddCoAuthors(IEnumerable<ResolvedCoAuthor> coAuthors);

    CommitMessage Build();

    void Clear();
}