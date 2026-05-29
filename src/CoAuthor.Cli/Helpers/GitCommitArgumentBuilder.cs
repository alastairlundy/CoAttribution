/*
    CoAuthorCli
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Text;

namespace CoAuthor.Cli.Helpers;

public static class GitCommitArgumentBuilder
{
    public static string CreateCommitArgs(ICommitMessageBuilder commitMessageBuilder)
    {
        CommitMessage result = commitMessageBuilder.Build();
        
        StringBuilder stringBuilder = new();

        stringBuilder.Append("commit -m ");
        stringBuilder.Append('"');
        stringBuilder.Append(result.Message);
        stringBuilder.Append('"');
        stringBuilder.Append(" --trailer");
        stringBuilder.Append('"');
        stringBuilder.Append(result.Trailer);
        stringBuilder.Append('"');

        return stringBuilder.ToString();
    }
}