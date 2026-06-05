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
using CoAttribution.Lib.Logic;

namespace CoAttribution.Cli.Commands;

[CliCommand(Name = "commit", Parent = typeof(RootCommand))]
public class CommitCommand
{
    private readonly IGitCoAuthorInfoProvider _coAuthorProvider;
    private readonly IConfigResolver _configResolver;
    private readonly ICommitMessageBuilder _commitMessageBuilder;
    private readonly IConfiguration _configuration;

    public CommitCommand(IGitCoAuthorInfoProvider coAuthorProvider, IConfigResolver configResolver,
        ICommitMessageBuilder commitMessageBuilder, IConfiguration configuration)
    {
        _coAuthorProvider = coAuthorProvider;
        _configResolver = configResolver;
        _commitMessageBuilder = commitMessageBuilder;
        _configuration = configuration;
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
        AppConfig configuration = await _configResolver.ResolveAppConfig(_configuration, cliContext.CancellationToken);

        FileInfo authorsFile = await FileHelper.ResolveAuthorTomlFileAsync(configuration, cliContext.CancellationToken);

        _commitMessageBuilder.SetSubject(SubjectMessage);
        _commitMessageBuilder.SetBody(BodyMessage);
        
        
        try
        {
            GitCoAuthor[] storedCoAuthors = await  _coAuthorProvider.GetCoAuthorsAsync(cliContext.CancellationToken);

            if (DefaultIds.Length != 0 && AssistIds.Length != 0 && CoAuthorIds.Length != 0)
            {
                KeyValuePair<GitCoAuthor, AttributionType>[] actualCoAuthors = CoAuthorResolver.ResolveCoAuthorsByAttributionType(storedCoAuthors,
                    DefaultIds, CoAuthorIds, AssistIds);
            
                foreach (KeyValuePair<GitCoAuthor, AttributionType> coAuthorPair in actualCoAuthors)
                {
                    _commitMessageBuilder.AddCoAuthorById(coAuthorPair.Key,
                        coAuthorPair.Value == AttributionType.DefaultOrCoAuthor
                            ? AttributionType.CoAuthor
                            : coAuthorPair.Value);
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