/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Cli.Commands;

[CliCommand(Name = "commit", Parent = typeof(RootCommand))]
public class CommitCommand
{
    private readonly ICommitOrchestrator _commitOrchestrator;

    public CommitCommand(ICommitOrchestrator commitOrchestrator)
    {
        _commitOrchestrator = commitOrchestrator;
    }
 
    [CliOption(Name = "message", Alias = "m", Required = true, Arity = CliArgumentArity.ExactlyOne)]
    public string SubjectMessage { get; set; } = "";
    
    [CliOption(Name = "body", Alias = "b", Required = false, Arity = CliArgumentArity.ExactlyOne)]
    public string BodyMessage { get; set; } = "";
    
    [CliOption(Name = "with", Required = false, Arity = CliArgumentArity.OneOrMore)]
    public string[] DefaultIds { get; set; } = [];
    
    [CliOption(Name = "coauthor", Required = false, Arity = CliArgumentArity.OneOrMore)]
    public string[] CoAuthorIds  { get; set; } = [];
    
    [CliOption(Name = "assist", Required = false, Arity = CliArgumentArity.OneOrMore)]
    public string[] AssistIds { get; set; } = [];
    
    [CliOption(Name = "verbose", Alias =  "v", Required = false)]
    public bool Verbose { get; set; } = false;
    
    public async Task<int> RunAsync(CliContext cliContext)
    {
        try
        {
            CommitMessage commitMessage = await _commitOrchestrator.BuildCommitMessageAsync(new CommitRequest(SubjectMessage, BodyMessage,
                    DefaultIds, CoAuthorIds, AssistIds),
                cliContext.CancellationToken);
            
            GitResult result = await _commitOrchestrator.ExecuteCommitAsync(commitMessage, cliContext.CancellationToken);
            
            Console.WriteLine();
            Console.WriteLine();

            return result.ExitCode;
        }
        catch(Exception exception)
        {
            await Console.Error.WriteLineAsync(Resources.Commands_Commit_Failed_Generic);
          
            if (Verbose)
            {
                await Console.Error.WriteLineAsync();
                throw;
            }
            
            await Console.Error.WriteLineAsync(Resources.Commands_Exceptions_Details + exception.Message);
            return 1;
        }
    }
}