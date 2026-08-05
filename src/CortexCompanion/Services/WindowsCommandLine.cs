// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text;

namespace CortexCompanion.Services;

/// <summary>Serializes an argument list according to the Windows command-line quoting contract.</summary>
public static class WindowsCommandLine
{
    /// <summary>Joins arguments without passing them through a command shell.</summary>
    public static string Join(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        return string.Join(" ", arguments.Select(Quote));
    }

    private static string Quote(string argument)
    {
        ArgumentNullException.ThrowIfNull(argument);
        if (argument.Length > 0 &&
            argument.IndexOfAny([' ', '\t', '\n', '\v', '"']) < 0)
        {
            return argument;
        }

        StringBuilder result = new();
        result.Append('"');
        int backslashes = 0;
        foreach (char character in argument)
        {
            if (character == '\\')
            {
                backslashes++;
                continue;
            }

            if (character == '"')
            {
                result.Append('\\', backslashes * 2 + 1);
                result.Append('"');
                backslashes = 0;
                continue;
            }

            result.Append('\\', backslashes);
            backslashes = 0;
            result.Append(character);
        }

        result.Append('\\', backslashes * 2);
        result.Append('"');
        return result.ToString();
    }
}
