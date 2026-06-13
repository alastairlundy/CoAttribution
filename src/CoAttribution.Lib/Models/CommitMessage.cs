/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Text;

namespace CoAttribution.Lib.Models;

public record CommitMessage(string Subject, IReadOnlyList<string> BodyLines, IReadOnlyList<ResolvedCoAuthor> CoAuthors)
{
    public (string message, string trailer) ToGitFormat() 
        => (BuildMessageBody(), BuildTrailer());

    public string ToConsoleFormat()
    {
        StringBuilder stringBuilder = new();

        (string message, string trailer) gitFormat = ToGitFormat();
        
        stringBuilder.AppendLine(gitFormat.message);
        stringBuilder.AppendLine();
        stringBuilder.Append(gitFormat.trailer);
        
        return stringBuilder.ToString();
    }

    public override string ToString()
        => ToConsoleFormat();
    
    private string BuildTrailer()
    {
        StringBuilder trailerBuilder = new();

        if (CoAuthors.Count > 0)
        {
            foreach (ResolvedCoAuthor resolvedCoAuthor in CoAuthors)
            {
                string attributionMessage = resolvedCoAuthor.Type == AttributionType.CoAuthor
                    ? Resources.CommitTrailers_CoAuthoredBy
                    : Resources.CommitTrailers_AssistedByAgent;

                trailerBuilder.Append($"{attributionMessage}: ");
                trailerBuilder.AppendLine(resolvedCoAuthor.Author.ToString());
            }
        }

        return trailerBuilder.ToString();
    }
    
    private string BuildMessageBody()
    {
        StringBuilder messageBuilder = new();
        
        messageBuilder.AppendLine(Subject);

        string bodyMessage = string.Join(Environment.NewLine, BodyLines);
        
        if (!string.IsNullOrEmpty(bodyMessage)) 
            messageBuilder.AppendLine(bodyMessage);
        
        return messageBuilder.ToString();
    }
}