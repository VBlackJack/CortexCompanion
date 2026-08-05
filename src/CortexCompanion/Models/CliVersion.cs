// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Models;

/// <summary>
/// Represents the observed Cortex CalVer format YYYY.MMDD.XX.
/// </summary>
public readonly record struct CliVersion(int Year, int Month, int Day, int Revision) : IComparable<CliVersion>
{
    /// <summary>Returns whether the left version is older than the right version.</summary>
    public static bool operator <(CliVersion left, CliVersion right) => left.CompareTo(right) < 0;

    /// <summary>Returns whether the left version is no newer than the right version.</summary>
    public static bool operator <=(CliVersion left, CliVersion right) => left.CompareTo(right) <= 0;

    /// <summary>Returns whether the left version is newer than the right version.</summary>
    public static bool operator >(CliVersion left, CliVersion right) => left.CompareTo(right) > 0;

    /// <summary>Returns whether the left version is no older than the right version.</summary>
    public static bool operator >=(CliVersion left, CliVersion right) => left.CompareTo(right) >= 0;

    /// <inheritdoc />
    public int CompareTo(CliVersion other)
    {
        int yearComparison = Year.CompareTo(other.Year);
        if (yearComparison != 0)
        {
            return yearComparison;
        }

        int monthComparison = Month.CompareTo(other.Month);
        if (monthComparison != 0)
        {
            return monthComparison;
        }

        int dayComparison = Day.CompareTo(other.Day);
        return dayComparison != 0 ? dayComparison : Revision.CompareTo(other.Revision);
    }

    /// <inheritdoc />
    public override string ToString() => $"{Year:D4}.{Month:D2}{Day:D2}.{Revision:D2}";
}
