/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using CliInvoke;
using CliInvoke.Core;
using CoAttribution.Cli.Abstractions;
using CoAttribution.Cli.Helpers;
using CoAttribution.Lib.Models.DTOs;

namespace CoAttribution.Cli.Commands;

[CliCommand(Name = "commit", Parent = typeof(RootCommand))]
public class CommitCommand
{
    private readonly IAuthorRegistry _authorRegistry;
    private readonly ICoAuthorResolver _coAuthorResolver;
    private readonly ICommitMessageBuilder _commitMessageBuilder;

    public CommitCommand(IAuthorRegistry authorRegistry,
        ICoAuthorResolver coAuthorResolver,
        ICommitMessageBuilder commitMessageBuilder)
    {
        _authorRegistry = authorRegistry;
        _coAuthorResolver = coAuthorResolver;
        _commitMessageBuilder = commitMessageBuilder;
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
        GitCoAuthorConfig config = await  _authorRegistry.GetAuthorConfigAsync(cliContext.CancellationToken);

        GitCoAuthor[] storedCoAuthors = config.GetCoAuthors();

        
        _commitMessageBuilder.SetSubject(SubjectMessage);
        _commitMessageBuilder.SetBody(BodyMessage);
        
        
        try
        {
            if (DefaultIds.Length != 0 && AssistIds.Length != 0 && CoAuthorIds.Length != 0)
            {
                ResolvedCoAuthor[] actualCoAuthors = _coAuthorResolver.ResolveCoAuthors(new CoAuthorResolutionRequest(storedCoAuthors,
                    DefaultIds, CoAuthorIds, AssistIds));
            
                foreach (ResolvedCoAuthor coAuthorPair in actualCoAuthors)
                {
                    _commitMessageBuilder.AddCoAuthorById(coAuthorPair.Author,
                        coAuthorPair.Type == AttributionType.DefaultOrCoAuthor
                            ? AttributionType.CoAuthor
                            : coAuthorPair.Type);
                }
            }

            BufferedProcessResult result = await CliRun.RunBufferedAsync(
                "git", GitCommitArgumentBuilder.CreateCommitArgs(_commitMessageBuilder),
                cancellationToken: cliContext.CancellationToken);
            
            Console.WriteLine();
            Console.WriteLine();

            return result.ExitCode;
        }
        catch(Exception exception)
        {
            Console.WriteLine(Resources.Commands_Commit_Failed_Generic, _commitMessageBuilder.ToString());
          
            if (Verbose)
            {
                Console.WriteLine();
                throw;
            }
            
            Console.WriteLine(Resources.Commands_Exceptions_Details + exception.Message);
            return 1;
        }
    }
}