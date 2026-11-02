/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CoAttribution.Cli.HostResolution;

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

            // Surface git's own stdout and stderr verbatim: on success the user
            // sees git's commit summary; on failure they see git's actual error
            // (hooks, staging, identity problems) instead of a bare exit code.
            if (!string.IsNullOrWhiteSpace(result.StandardOutput))
                await Console.Out.WriteLineAsync(result.StandardOutput);
            if (!string.IsNullOrWhiteSpace(result.StandardError))
                await Console.Error.WriteLineAsync(result.StandardError);

            Console.WriteLine();
            Console.WriteLine();

            return result.ExitCode;
        }
        catch (MissingHostBlockException mhbEx)
        {
            MissingHostBlockDiagnosticFormatter formatter = new();

            foreach (MissingHostBlockDiagnostic diagnostic in mhbEx.Diagnostics)
            {
                await Console.Error.WriteLineAsync(formatter.Format(diagnostic));
                await Console.Error.WriteLineAsync();
            }

            return 1;
        }
        catch (UnknownCoAuthorException unknownEx)
        {
            // Unknown author IDs are a user error that must be loud: show
            // every unknown ID plus the registered IDs, regardless of --verbose.
            await Console.Error.WriteLineAsync(unknownEx.Message);

            return 1;
        }
        catch(Exception exception)
        {
            await Console.Error.WriteLineAsync(string.Format(Resources.Commands_Commit_Failed_Generic, SubjectMessage));
          
            if (Verbose)
            {
                await Console.Error.WriteLineAsync();
                
                await Console.Error.WriteLineAsync(Resources.Commands_Exceptions_Details + exception.Message);
            }
            
            return 1;
        }
    }
}