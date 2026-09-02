// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text.RegularExpressions;

namespace CortexCompanion.Tests.Themes;

/// <summary>Keeps layout and typography values in the token dictionary, never in a view.</summary>
[TestClass]
public sealed partial class DesignTokenTests
{
    [TestMethod]
    public void ViewsCarryNoRawLayoutOrTypographyValues()
    {
        string repositoryRoot = FindRepositoryRoot();
        string applicationRoot = Path.Combine(repositoryRoot, "src", "CortexCompanion");
        IEnumerable<string> paths = Directory
            .EnumerateFiles(Path.Combine(applicationRoot, "Views"), "*.xaml")
            .Append(Path.Combine(applicationRoot, "MainWindow.xaml"));

        List<string> offenders = [];
        foreach (string path in paths)
        {
            foreach (Match match in RawLayoutValuePattern().Matches(File.ReadAllText(path)))
            {
                offenders.Add($"{Path.GetFileName(path)}: {match.Value}");
            }
        }

        Assert.IsEmpty(
            offenders,
            "Raw layout values must move into Themes/CommonControls.xaml as named tokens: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void EveryTokenReferencedByAViewExists()
    {
        string repositoryRoot = FindRepositoryRoot();
        string applicationRoot = Path.Combine(repositoryRoot, "src", "CortexCompanion");
        string tokens = File.ReadAllText(
            Path.Combine(applicationRoot, "Themes", "CommonControls.xaml")) +
            File.ReadAllText(Path.Combine(applicationRoot, "Themes", "DarkTheme.xaml"));
        HashSet<string> declared = [.. DeclaredKeyPattern().Matches(tokens).Select(match => match.Groups[1].Value)];

        List<string> missing = [];
        IEnumerable<string> paths = Directory
            .EnumerateFiles(Path.Combine(applicationRoot, "Views"), "*.xaml")
            .Append(Path.Combine(applicationRoot, "MainWindow.xaml"));
        foreach (string path in paths)
        {
            string content = File.ReadAllText(path);
            foreach (Match match in ResourceReferencePattern().Matches(content))
            {
                string key = match.Groups[2].Value;
                if (!declared.Contains(key))
                {
                    missing.Add($"{Path.GetFileName(path)}: {key}");
                }
            }
        }

        Assert.IsEmpty(missing, "Views reference undeclared theme resources: " + string.Join(", ", missing));
    }

    [GeneratedRegex(
        "(Margin|Padding|Width|Height|MinWidth|MinHeight|MaxWidth|MaxHeight|FontSize|" +
        "BorderThickness|CornerRadius)=\"[0-9][^\"]*\"",
        RegexOptions.CultureInvariant)]
    private static partial Regex RawLayoutValuePattern();

    [GeneratedRegex("x:Key=\"([^\"]+)\"", RegexOptions.CultureInvariant)]
    private static partial Regex DeclaredKeyPattern();

    [GeneratedRegex("""\{(StaticResource|DynamicResource) ([A-Za-z0-9_]+)\}""", RegexOptions.CultureInvariant)]
    private static partial Regex ResourceReferencePattern();

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
