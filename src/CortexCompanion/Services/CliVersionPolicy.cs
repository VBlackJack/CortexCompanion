// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Text.RegularExpressions;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>
/// Parses and compares the measured Cortex CalVer format YYYY.MMDD.XX.
/// </summary>
public sealed partial class CliVersionPolicy
{
    private readonly Regex _versionPattern = VersionPattern();

    /// <summary>
    /// Parses exactly one CalVer token after trimming surrounding whitespace; labels or extra tokens are rejected.
    /// </summary>
    public bool TryParse(string output, out CliVersion version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(output))
        {
            return false;
        }

        Match match = _versionPattern.Match(output.Trim());
        if (!match.Success ||
            !int.TryParse(match.Groups["year"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int year) ||
            !int.TryParse(match.Groups["month"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int month) ||
            !int.TryParse(match.Groups["day"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int day) ||
            !int.TryParse(match.Groups["revision"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int revision))
        {
            return false;
        }

        try
        {
            _ = new DateOnly(year, month, day);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        version = new CliVersion(year, month, day, revision);
        return true;
    }

    /// <summary>Returns true when the detected version is equal to or newer than the minimum.</summary>
    public static bool IsSupported(CliVersion detected, CliVersion minimum) => detected >= minimum;

    [GeneratedRegex("^(?<year>\\d{4})\\.(?<month>\\d{2})(?<day>\\d{2})\\.(?<revision>\\d{2})$", RegexOptions.CultureInvariant)]
    private static partial Regex VersionPattern();
}
