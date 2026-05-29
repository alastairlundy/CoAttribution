/*
    CoAuthorLib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAuthorLib.Builders;

public interface ICommitMessageBuilder
{
    ICommitMessageBuilder SetSubject(string subject);
    
    ICommitMessageBuilder SetBody(string text);

    ICommitMessageBuilder AddBodyLine(string text);
    
    ICommitMessageBuilder AddCoAuthor(GitCoAuthor coAuthor, AttributionType attributionType);

    CommitMessage Build();
    
    string ToString();

    void Clear();
}