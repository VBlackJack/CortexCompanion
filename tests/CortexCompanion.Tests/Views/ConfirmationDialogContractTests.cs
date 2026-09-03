// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text.RegularExpressions;
using System.Xml.Linq;
using CortexCompanion.Views;

namespace CortexCompanion.Tests.Views;

/// <summary>Guards confirmation dialogs against unsafe result and fallback regressions.</summary>
[TestClass]
public sealed class ConfirmationDialogContractTests
{
    private static readonly Regex MessageBoxShowPattern = new(
        @"\bMessageBox\s*\.\s*Show\s*\(",
        RegexOptions.CultureInvariant);
    private static readonly Regex SafeDialogMappingPattern = new(
        @"ConfirmationDialog\.IsConfirmed\(\s*dialog\.ShowDialog\(\)\s*\)",
        RegexOptions.CultureInvariant);

    /// <summary>Ensures that null and false modal results can never authorize a mutation.</summary>
    [TestMethod]
    public void OnlyTrueDialogResultConfirms()
    {
        Assert.IsTrue(ConfirmationDialog.IsConfirmed(true));
        Assert.IsFalse(ConfirmationDialog.IsConfirmed(false));
        Assert.IsFalse(ConfirmationDialog.IsConfirmed(null));
    }

    /// <summary>Ensures service call sites retain the safe result mapper.</summary>
    [TestMethod]
    public void ConfirmationServicesUseSafeDialogResultMapping()
    {
        AssertSafeMapping("PageMutationConfirmationService.cs", 5);
        AssertSafeMapping("SchedulingConfirmationService.cs", 1);
    }

    /// <summary>Ensures the system fallback remains limited to fatal startup handling.</summary>
    [TestMethod]
    public void MessageBoxShowIsLimitedToFatalStartupFallback()
    {
        string sourceRoot = Path.Combine(FindRepositoryRoot(), "src", "CortexCompanion");
        List<string> sites = [];

        foreach (string path in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string source = File.ReadAllText(path);
            foreach (Match match in MessageBoxShowPattern.Matches(source))
            {
                sites.Add($"{Path.GetRelativePath(sourceRoot, path)}:{GetLineNumber(source, match.Index)}");
            }
        }

        Assert.HasCount(1, sites, $"Expected one fatal-startup MessageBox.Show site. Found: {string.Join(", ", sites)}");
        StringAssert.StartsWith(sites[0], "App.xaml.cs:");
    }

    /// <summary>Ensures Escape and the cancel button share the built-in WPF cancellation path.</summary>
    [TestMethod]
    public void CancelButtonIsTheOnlyCancelHandlerAndNoButtonIsDefault()
    {
        XDocument dialog = XDocument.Load(GetDialogPath());
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        XElement cancelButton = dialog.Descendants(presentation + "Button")
            .Single(element => string.Equals(
                (string?)element.Attribute(xaml + "Name"),
                "CancelButton",
                StringComparison.Ordinal));

        Assert.AreEqual("True", (string?)cancelButton.Attribute("IsCancel"));
        Assert.IsNull(cancelButton.Attribute("IsDefault"));
        Assert.IsNull(cancelButton.Attribute("Click"));

        foreach (XElement button in dialog.Descendants(presentation + "Button"))
        {
            Assert.AreNotEqual("True", (string?)button.Attribute("IsDefault"));
        }
    }

    private static void AssertSafeMapping(string fileName, int expectedCount)
    {
        string path = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CortexCompanion",
            "Services",
            fileName);
        string source = File.ReadAllText(path);
        MatchCollection safeMappings = SafeDialogMappingPattern.Matches(source);

        Assert.HasCount(
            expectedCount,
            safeMappings.Cast<Match>().ToList(),
            $"{fileName} must map every dialog result through IsConfirmed.");
        Assert.IsFalse(source.Contains("ShowDialog() != false", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("ShowDialog() ?? true", StringComparison.Ordinal));
    }

    private static int GetLineNumber(string source, int index) =>
        source.AsSpan(0, index).Count('\n') + 1;

    private static string GetDialogPath() => Path.Combine(
        FindRepositoryRoot(),
        "src",
        "CortexCompanion",
        "Views",
        "ConfirmationDialog.xaml");

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
