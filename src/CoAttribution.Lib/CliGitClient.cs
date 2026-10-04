/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */


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
        ProcessConfiguration processConfiguration = new(
            OperatingSystem.IsWindows() ? "git.exe" : "git",
            CreateCommitArgs(message));
        
        BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(
            processConfiguration, cancellationToken: cancellationToken);
        
        return new GitResult(result.ExitCode, result.StandardOutput, result.StandardError);
    }
    
    private static List<string> CreateCommitArgs(CommitMessage commitMessage)
    {
        // One argv entry per token via CliInvoke's ArgumentList constructor: git
        // receives each element unmodified, so quotes or other special characters
        // in the message cannot break or hijack the command line.
        (string message, string trailer) gitFormat = commitMessage.ToGitFormat();

        List<string> args = ["commit", "-m", gitFormat.message];

        // Emit one --trailer entry per trailer line. Git rejects a --trailer that
        // bundles multiple values into a single argument (exit code 129).
        foreach (string line in gitFormat.trailer
                     .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            args.Add("--trailer");
            args.Add(line);
        }

        return args;
    }
}