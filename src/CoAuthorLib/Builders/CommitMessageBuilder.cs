/*
    CoAuthorLib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Text;

namespace CoAuthorLib.Builders;

public class CommitMessageBuilder : ICommitMessageBuilder
{
    private readonly List<string> _bodyTextLines;
    private readonly List<(GitCoAuthor coAuthor, AttributionType attributionType)> _coAuthors;
    
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

    public ICommitMessageBuilder AddCoAuthor(GitCoAuthor coAuthor, 
        AttributionType attributionType)
    {
        ArgumentNullException.ThrowIfNull(coAuthor);
        
        ArgumentException.ThrowIfNullOrEmpty(coAuthor.CoAuthorId);
        ArgumentException.ThrowIfNullOrEmpty(coAuthor.Name);
        ArgumentException.ThrowIfNullOrEmpty(coAuthor.Email);
        
        _coAuthors.Add((coAuthor, attributionType));
        
        return this;
    }

    public new string ToString()
    {
        StringBuilder stringBuilder = new();
        
        stringBuilder.AppendLine(_subject);

        string bodyMessage = string.Join(Environment.NewLine, _bodyTextLines);
        
        ArgumentException.ThrowIfNullOrEmpty(bodyMessage);
        
        if (!string.IsNullOrEmpty(bodyMessage)) 
            stringBuilder.AppendLine(bodyMessage);
        
        if (_coAuthors.Count > 0)
        {
            stringBuilder.AppendLine();
            
            foreach ((GitCoAuthor coAuthor, AttributionType attributionType) coAuthorTuple in _coAuthors)
            {
                string attributionMessage = coAuthorTuple.attributionType == AttributionType.CoAuthor
                    ? Resources.CommitTrailers_CoAuthoredBy
                    : Resources.CommitTrailers_AssistedByAgent;
                
                stringBuilder.Append($"{attributionMessage}: ");
                stringBuilder.AppendLine(coAuthorTuple.coAuthor.ToString());
            }
        }
        
        return stringBuilder.ToString();
    }

    public void Clear()
    {
        _subject = string.Empty;
        _bodyTextLines.Clear();
        
    }
}