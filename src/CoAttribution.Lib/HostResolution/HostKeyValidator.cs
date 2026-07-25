/*
    CoAttribution.Lib
    Copyright (c) Alastair Lundy 2026
 
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Text.RegularExpressions;

namespace CoAttribution.Lib.HostResolution;

// ReSharper disable once PartialTypeWithSinglePart
public static partial class HostKeyValidator
{
    public const string Pattern = "^[a-z]+$";

    [GeneratedRegex(Pattern)]
    private static partial Regex HostKeyRegex();

    public static bool IsValid(string? key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        return HostKeyRegex().IsMatch(key);
    }

    public static bool TryValidate(string? key, out string? error)
    {
        if (key is null)
        {
            error = "Host key cannot be null.";
            return false;
        }

        if (key.Length == 0)
        {
            error = "Host key cannot be empty.";
            return false;
        }

        if (HostKeyRegex().IsMatch(key))
        {
            error = null;
            return true;
        }

        string[] invalidCharacters = key
            .Where(static c => !IsLowercaseAsciiLetter(c))
            .Distinct()
            .Select(static c => $"'{c}'")
            .ToArray();

        error = invalidCharacters.Length > 0
            ? $"Host key '{key}' is invalid: only lowercase ASCII letters (a-z) are allowed. Offending character(s): {string.Join(", ", invalidCharacters)}."
            : $"Host key '{key}' is invalid: it must match the pattern {Pattern}.";

        return false;
    }

    private static bool IsLowercaseAsciiLetter(char c) => c >= 'a' && c <= 'z';
}
