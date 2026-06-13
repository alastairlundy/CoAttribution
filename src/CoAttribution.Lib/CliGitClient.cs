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
        stringBuilder.Append(" --trailer");
        stringBuilder.Append('"');
        stringBuilder.Append(gitFormat.trailer);
        stringBuilder.Append('"');

        return stringBuilder.ToString();
    }
}