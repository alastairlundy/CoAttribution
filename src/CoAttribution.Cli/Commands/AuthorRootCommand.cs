/*
    CoAttribution
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace CoAttribution.Cli.Commands;

public class AuthorRootCommand
{
    /// <summary>
    /// Builds the <c>author</c> command and its three subcommands
    /// (<c>add</c>, <c>remove</c>, <c>list</c>).
    /// </summary>
    public static Command Create(IServiceProvider services)
    {
        Command command = new("author");
        command.Subcommands.Add(AddCoAuthorCommand.Create(services));
        command.Subcommands.Add(ListCoAuthorsCommand.Create(services));
        command.Subcommands.Add(RemoveCoAuthorCommand.Create(services));

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            AuthorRootCommand handler = ActivatorUtilities.CreateInstance<AuthorRootCommand>(services);

            return await handler.RunAsync(CliContext.From(parseResult, cancellationToken));
        });

        return command;
    }

    public Task<int> RunAsync(CliContext context)
    {
        context.ShowHelp();

        return Task.FromResult(1);
    }
}