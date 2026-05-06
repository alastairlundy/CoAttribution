/*
    CoAuthorCli
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
    .AddCommandLine(args)
    .AddEnvironmentVariables();

IConfiguration configuration = configurationBuilder.Build();

Cli.Ext.ConfigureServices(services =>
{
    services.AddSingleton<IGitCoAuthorInfoProvider, GitCoAuthorInfoProvider>();
    services.AddSingleton<ICommitMessageBuilder, CommitMessageBuilder>();
    
    services.AddSingleton(configuration);
});

CliSettings settings = new()
{
    EnablePosixBundling = true,
    EnableDefaultExceptionHandler = true,
    
};

return await Cli.RunAsync<RootCommand>(args, settings);