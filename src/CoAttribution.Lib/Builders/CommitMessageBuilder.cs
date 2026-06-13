/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Lib.Builders;

public class CommitMessageBuilder : ICommitMessageBuilder
{
    private readonly List<string> _bodyTextLines;
    private readonly List<ResolvedCoAuthor> _coAuthors;
    
    private string _subject;
    
    public CommitMessageBuilder()
    {
        _coAuthors = [];
        _bodyTextLines = [];
        _subject = string.Empty;
    }
    
    public ICommitMessageBuilder SetSubject(string subject)
    {
        ArgumentNullException.ThrowIfNull(subject);
        
        _subject = subject;
        
        return this;
    }

    public ICommitMessageBuilder SetBody(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        
        _bodyTextLines.Clear();
        _bodyTextLines.Add(text);
        
        return this;
    }

    public ICommitMessageBuilder AddBodyLine(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        
        _bodyTextLines.Add(text);
        
        return this;
    }

    public ICommitMessageBuilder AddCoAuthorById(GitCoAuthor coAuthor, AttributionType attributionType)
    {
        ArgumentNullException.ThrowIfNull(coAuthor);
        
        _coAuthors.Add(new ResolvedCoAuthor(coAuthor, attributionType));
        
        return this;
    }

    public CommitMessage Build() => new(_subject, _bodyTextLines.AsReadOnly(),
        _coAuthors.AsReadOnly());

    public void Clear()
    {
        _subject = string.Empty;
        _bodyTextLines.Clear();
        _coAuthors.Clear();
    }
}