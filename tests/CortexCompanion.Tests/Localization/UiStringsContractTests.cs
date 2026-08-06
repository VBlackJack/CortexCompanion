// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

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
