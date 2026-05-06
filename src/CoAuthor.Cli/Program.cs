using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

Cli.Ext.ConfigureServices(services =>
{
    services.AddSingleton<IGitCoAuthorInfoProvider, GitCoAuthorInfoProvider>();
    services.AddSingleton<ICommitMessageBuilder, CommitMessageBuilder>();
});

CliSettings settings = new()
{
    EnablePosixBundling = true,
    EnableDefaultExceptionHandler = true,
    
};

return await Cli.RunAsync<RootCommand>(args, settings);