/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Text;

namespace CoAttribution.Lib.Builders;

public class CommitMessageBuilder : ICommitMessageBuilder
{
    private readonly ICoAuthorResolver _coAuthorResolver;
    private readonly List<string> _bodyTextLines;
    private readonly List<(GitCoAuthor coAuthor, AttributionType attributionType)> _coAuthors;
    
    private string _subject;
    
    public CommitMessageBuilder(ICoAuthorResolver coAuthorResolver)
    {
        _coAuthorResolver = coAuthorResolver;
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

    public ICommitMessageBuilder AddCoAuthorById(string coAuthorId)
    {
        ArgumentNullException.ThrowIfNull(coAuthorId);
        
        
        
        _coAuthors.Add((coAuthor, attributionType));
        
        return this;
    }

    public CommitMessage Build()
    {
        StringBuilder messageBuilder = new();
        StringBuilder trailerBuilder = new();
        
        messageBuilder.AppendLine(_subject);

        string bodyMessage = string.Join(Environment.NewLine, _bodyTextLines);
        
        ArgumentException.ThrowIfNullOrEmpty(bodyMessage);
        
        if (!string.IsNullOrEmpty(bodyMessage)) 
            messageBuilder.AppendLine(bodyMessage);
        
        if (_coAuthors.Count > 0)
        {
            messageBuilder.AppendLine();
            messageBuilder.AppendLine();
            
            foreach ((GitCoAuthor coAuthor, AttributionType attributionType) coAuthorTuple in _coAuthors)
            {
                string attributionMessage = coAuthorTuple.attributionType == AttributionType.CoAuthor
                    ? Resources.CommitTrailers_CoAuthoredBy
                    : Resources.CommitTrailers_AssistedByAgent;
                
                trailerBuilder.Append($"{attributionMessage}: ");
                trailerBuilder.AppendLine(coAuthorTuple.coAuthor.ToString());
            }
        }
        
        return new CommitMessage(messageBuilder.ToString(), trailerBuilder.ToString());
    }

    public void Clear()
    {
        _subject = string.Empty;
        _bodyTextLines.Clear();
    }
}