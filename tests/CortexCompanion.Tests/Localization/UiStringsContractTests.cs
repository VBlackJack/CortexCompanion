// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using CortexCompanion.Constants;
using CortexCompanion.Localization;

namespace CortexCompanion.Tests.Localization;

/// <summary>Guards localization resources against typography that violates repository conventions.</summary>
[TestClass]
public sealed class UiStringsContractTests
{
    private static readonly char[] BannedPunctuation =
    [
        '\u00A0',
        '\u00AB',
        '\u00BB',
        '\u2013',
        '\u2014',
        '\u2018',
        '\u2019',
        '\u201C',
        '\u201D',
        '\u2026',
    ];

    /// <summary>Ensures UI resources retain plain punctuation while preserving legitimate accents.</summary>
    [TestMethod]
    public void UiStringsContainsNoBannedPunctuation()
    {
        string resourcePath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CortexCompanion",
            "Localization",
            "UiStrings.resx");
        string content = File.ReadAllText(resourcePath);

        foreach (char bannedCharacter in BannedPunctuation)
        {
            Assert.IsFalse(
                content.Contains(bannedCharacter, StringComparison.Ordinal),
                $"UiStrings.resx contains banned punctuation U+{(int)bannedCharacter:X4}.");
        }
    }

    /// <summary>Ensures the timeout advice names the ceiling instead of promising more.</summary>
    /// <remarks>
    /// NormalizeCliTimeoutSeconds reverts any value outside CliTimeoutOptions to the
    /// default, so an unqualified "raise the timeout" cannot be followed past the
    /// highest option, and a hand-edited settings file silently lands four times lower.
    /// </remarks>
    [TestMethod]
    public void TimeoutAdviceStatesTheHighestSelectableTimeout()
    {
        string message = UiStrings.FormatPagesCliTimedOut(AppConstants.MaximumCliTimeoutSeconds);

        StringAssert.Contains(
            message,
            AppConstants.MaximumCliTimeoutSeconds.ToString(CultureInfo.CurrentCulture),
            "The timeout advice must say where raising the timeout stops.");
    }

    /// <summary>Ensures no exposed string silently degrades to its own resource key.</summary>
    /// <remarks>
    /// UiStrings falls back to the key name when a resource is missing, so a typo
    /// or a deleted entry ships as visible English key text inside a French UI.
    /// </remarks>
    [TestMethod]
    public void EveryExposedStringResolvesToRealResourceText()
    {
        List<string> unresolved = [];
        foreach (PropertyInfo property in typeof(UiStrings)
                     .GetProperties(BindingFlags.Public | BindingFlags.Static)
                     .Where(candidate => candidate.PropertyType == typeof(string)))
        {
            string? value = (string?)property.GetValue(null);
            if (string.IsNullOrWhiteSpace(value) ||
                string.Equals(value, property.Name, StringComparison.Ordinal))
            {
                unresolved.Add(property.Name);
            }
        }

        Assert.IsEmpty(unresolved, "UiStrings members without a resource: " + string.Join(", ", unresolved));
    }

    /// <summary>Ensures every declared resource is actually reachable from the API.</summary>
    [TestMethod]
    public void EveryDeclaredResourceIsExposedByUiStrings()
    {
        string source = string.Join(Environment.NewLine, Directory.EnumerateFiles(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CortexCompanion",
            "Localization"), "UiStrings*.cs").Select(File.ReadAllText));
        string resource = File.ReadAllText(ResourcePath());

        List<string> orphans = [];
        foreach (Match match in Regex.Matches(
                     resource,
                     "<data name=\"([^\"]+)\"",
                     RegexOptions.CultureInvariant))
        {
            string key = match.Groups[1].Value;
            if (!source.Contains($"nameof({key})", StringComparison.Ordinal) &&
                !source.Contains($"GetString(\"{key}\")", StringComparison.Ordinal))
            {
                orphans.Add(key);
            }
        }

        Assert.IsEmpty(orphans, "Resources no member exposes: " + string.Join(", ", orphans));
    }

    private static string ResourcePath() => Path.Combine(
        FindRepositoryRoot(),
        "src",
        "CortexCompanion",
        "Localization",
        "UiStrings.resx");

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located.");
    }
}
