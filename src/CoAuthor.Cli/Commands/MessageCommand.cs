/*
    CoAuthorCli
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */


namespace CoAuthor.Cli.Commands;

[CliCommand(Name = "message", Parent = typeof(RootCommand))]
public class MessageCommand
{
    private readonly ICommitMessageBuilder _commitMessageBuilder;
    private readonly IGitCoAuthorInfoProvider _gitCoAuthorInfoProvider;
    private readonly IConfiguration _configuration;

    public MessageCommand(ICommitMessageBuilder commitMessageBuilder, IGitCoAuthorInfoProvider gitCoAuthorInfoProvider,
        IConfiguration configuration)
    {
        _commitMessageBuilder = commitMessageBuilder;
        _gitCoAuthorInfoProvider = gitCoAuthorInfoProvider;
        _configuration = configuration;
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
        FileInfo configFile = FileHelper.ResolveConfigFile(_configuration);

        FileInfo authorsFile = await FileHelper.ResolveAuthorTomlFileAsync(configFile, cliContext.CancellationToken);
        
        try
        {
            GitCoAuthor[] storedCoAuthors = await _gitCoAuthorInfoProvider.GetCoAuthorsAsync(authorsFile.FullName,
                cliContext.CancellationToken);
            
            _commitMessageBuilder.SetSubject(SubjectMessage);
            _commitMessageBuilder.SetBody(BodyMessage);

            KeyValuePair<GitCoAuthor, AttributionType>[] actualCoAuthors = CoAuthorResolver.ResolveCoAuthorsByAttributionType(storedCoAuthors,
                DefaultIds, CoAuthorIds, AssistIds);
            
            foreach (KeyValuePair<GitCoAuthor, AttributionType> coAuthorPair in actualCoAuthors)
            {
                _commitMessageBuilder.AddCoAuthor(coAuthorPair.Key,
                    coAuthorPair.Value == AttributionType.DefaultOrCoAuthor
                        ? AttributionType.CoAuthor
                        : coAuthorPair.Value);
            }
            
            string builtCommitMessage = _commitMessageBuilder.ToString();

            await Console.Out.WriteLineAsync(builtCommitMessage);
            
            return 0;
        }
        catch(Exception exception)
        {
            Console.WriteLine(Resources.Commands_Message_Failed, _commitMessageBuilder.ToString());
          
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