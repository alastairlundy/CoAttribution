/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */


using System.Text;
using CliInvoke.Core;

namespace CoAttribution.Lib;

public class CliGitClient : IGitClient
{
    private readonly IProcessInvoker _processInvoker;

    public CliGitClient(IProcessInvoker processInvoker)
    {
        _processInvoker = processInvoker;
    }
    
    public async Task<GitResult> CommitAsync(CommitMessage message, CancellationToken cancellationToken)
    {
        using ProcessConfiguration processConfiguration = new(OperatingSystem.IsWindows() ? "git.exe" : "git",
            CreateCommitArgs(message));
        
        BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(
            processConfiguration, cancellationToken: cancellationToken);
        
        return new GitResult(result.ExitCode, result.StandardOutput, result.StandardError);
    }
    
    private static string CreateCommitArgs(CommitMessage commitMessage)
    {
        StringBuilder stringBuilder = new();

        (string message, string trailer) gitFormat = commitMessage.ToGitFormat();

        stringBuilder.Append("commit -m ");
        stringBuilder.Append('"');
        stringBuilder.Append(gitFormat.message);
        stringBuilder.Append('"');

        // Emit one --trailer "<value>" per trailer line. A missing space before
        // the value (or collapsing all trailers into a single value) makes git
        // reject the argument and exit with code 129.
        foreach (string line in gitFormat.trailer
                     .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            stringBuilder.Append(" --trailer \"");
            stringBuilder.Append(line);
            stringBuilder.Append('"');
        }

        return stringBuilder.ToString();
    }
}