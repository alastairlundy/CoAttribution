/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Text;

namespace CoAttribution.Lib.Models;

public record CommitMessage(string Message, string Trailer)
{
    public override string ToString()
    {
        StringBuilder stringBuilder = new();
        
        stringBuilder.AppendLine(Message);

        stringBuilder.AppendLine();
        stringBuilder.AppendLine();
        
        stringBuilder.Append(Trailer);
        
        return stringBuilder.ToString();
    }
}