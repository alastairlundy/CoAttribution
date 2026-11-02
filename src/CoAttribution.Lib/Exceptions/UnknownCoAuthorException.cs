/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace CoAttribution.Lib.Exceptions;

/// <summary>
/// Thrown when an attribution request references author IDs that are not
/// registered in the author registry. Attribution must never be silently
/// skipped: this exception lists every unknown ID so the caller can fail
/// loudly instead of committing unattributed.
/// </summary>
public class UnknownCoAuthorException : Exception
{
    public string[] UnknownIds { get; }

    public string[] AvailableIds { get; }

    public UnknownCoAuthorException(string[] unknownIds, string[] availableIds)
        : base(BuildMessage(unknownIds, availableIds))
    {
        UnknownIds = [..unknownIds];
        AvailableIds = [..availableIds];
    }

    private static string BuildMessage(string[] unknownIds, IReadOnlyList<string> availableIds)
    {
        string unknownList = string.Join(", ", unknownIds.Select(id => $"'{id}'"));
        string availableList = availableIds.Count > 0
            ? string.Join(", ", availableIds)
            : "(no authors registered)";

        var builder = new System.Text.StringBuilder(
            $"Unknown co-author ID(s): {unknownList}. Registered author IDs: {availableList}.");

        foreach (string unknownId in unknownIds)
        {
            string? closestMatch = availableIds
                .Select(knownId => (KnownId: knownId, Distance: LevenshteinDistance(unknownId, knownId)))
                .Where(match => match.Distance <= MaxTypoDistance)
                .OrderBy(match => match.Distance)
                .Select(match => match.KnownId)
                .FirstOrDefault();

            if (closestMatch is not null)
                builder.Append($" Did you mean '{closestMatch}'? (for '{unknownId}')");
        }

        return builder.ToString();
    }

    private const int MaxTypoDistance = 2;

    private static int LevenshteinDistance(string first, string second)
    {
        int[,] distances = new int[first.Length + 1, second.Length + 1];

        for (int i = 0; i <= first.Length; i++)
            distances[i, 0] = i;
        for (int j = 0; j <= second.Length; j++)
            distances[0, j] = j;

        for (int i = 1; i <= first.Length; i++)
        {
            for (int j = 1; j <= second.Length; j++)
            {
                int substitutionCost = first[i - 1] == second[j - 1] ? 0 : 1;

                distances[i, j] = Math.Min(
                    Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + substitutionCost);
            }
        }

        return distances[first.Length, second.Length];
    }
}
