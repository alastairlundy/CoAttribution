/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Cli.Commands;

[CliCommand(Name = "message", Parent = typeof(RootCommand))]
public class MessageCommand
{
    private readonly ICommitOrchestrator _commitOrchestrator;
    
    public MessageCommand(ICommitOrchestrator commitOrchestrator)
    {
        _commitOrchestrator = commitOrchestrator;
    }
    
    [CliOption(Name = "message", Alias = "m", Required = true, Arity = CliArgumentArity.ExactlyOne)]
    public string SubjectMessage { get; set; } = "";
    
    [CliOption(Name = "body", Alias = "b", Required = false, Arity = CliArgumentArity.ExactlyOne)]
    public string BodyMessage { get; set; } = "";
    
    [CliOption(Name = "with", Required = true, Arity = CliArgumentArity.OneOrMore)]
    public string[] DefaultIds { get; set; } = [];
    
    [CliOption(Name = "coauthor", Required = false, Arity = CliArgumentArity.OneOrMore)]
    public string[] CoAuthorIds  { get; set; } = [];
    
    [CliOption(Name = "assist", Required = false, Arity = CliArgumentArity.OneOrMore)]
    public string[] AssistIds { get; set; } = [];
    
    [CliOption(Name = "verbose", Alias =  "v", Required = false)]
    // ReSharper disable once RedundantDefaultMemberInitializer
    public bool Verbose { get; set; } = false;
    
    public async Task<int> RunAsync(CliContext cliContext)  
    {
        try
        {
            CommitMessage commitMessage = await _commitOrchestrator.BuildCommitMessageAsync(new CommitRequest(SubjectMessage, BodyMessage,
                    DefaultIds, CoAuthorIds, AssistIds),
                cliContext.CancellationToken);

            await Console.Out.WriteLineAsync();
            await Console.Out.WriteLineAsync();

            await Console.Out.WriteLineAsync(commitMessage.ToString());

            return 0;
        }
        catch(Exception exception)
        {
            await Console.Error.WriteLineAsync(Resources.Commands_Message_Failed);
          
            if (Verbose)
            {
                await Console.Error.WriteLineAsync();
                
                await Console.Error.WriteLineAsync(Resources.Commands_Exceptions_Details + exception.Message);
            }
            
            return 1;
        }
    }
}