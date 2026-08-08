// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text.RegularExpressions;
using System.Xml.Linq;
using CortexCompanion.Localization;
using CortexCompanion.Services;

namespace CortexCompanion.Tests.Views;

/// <summary>Guards first-run ordering, accessibility, and CLI ownership boundaries.</summary>
[TestClass]
public sealed partial class SettingsUxContractTests
{
    [TestMethod]
    public void ShellIsShownBeforeSettingsLoadAndHandshakeInitialization()
    {
        string source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CortexCompanion",
            "App.xaml.cs"));

        int showIndex = source.IndexOf("window.Show();", StringComparison.Ordinal);
        int loadIndex = source.IndexOf("settingsStore.LoadAsync", StringComparison.Ordinal);
        int initializeIndex = source.IndexOf("viewModel.InitializeAsync", StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, showIndex);
        Assert.IsGreaterThan(showIndex, loadIndex);
        Assert.IsGreaterThan(showIndex, initializeIndex);
    }

    [TestMethod]
    public void SettingsWorkflowContainsNoDirectTomlWritePath()
    {
        string root = FindRepositoryRoot();
        string viewModel = File.ReadAllText(Path.Combine(
            root,
            "src",
            "CortexCompanion",
            "ViewModels",
            "SettingsViewModel.cs"));
        string client = File.ReadAllText(Path.Combine(
            root,
            "src",
            "CortexCompanion",
            "Services",
            "CortexConfigClient.cs"));

        Assert.IsFalse(viewModel.Contains(".toml", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(viewModel.Contains("File.Write", StringComparison.Ordinal));
        Assert.IsFalse(client.Contains(".toml", StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(client, "[\"config\", \"get\", \"--json\"]");
        StringAssert.Contains(client, "\"--expected-hash\"");
        StringAssert.Contains(client, "\"--expect-absent\"");
    }

    [TestMethod]
    public void SettingsInteractiveControlsHaveAccessibleNamesOrLabels()
    {
        XDocument document = XDocument.Load(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CortexCompanion",
            "Views",
            "SettingsView.xaml"));
        XElement[] controls = document
            .Descendants()
            .Where(element => element.Name.LocalName is "Button" or "TextBox" or "PasswordBox")
            .ToArray();

        foreach (XElement control in controls)
        {
            bool accessible = control.Attribute("AutomationProperties.Name") is not null ||
                control.Attribute("AutomationProperties.LabeledBy") is not null;
            Assert.IsTrue(accessible, $"{control.Name.LocalName} is missing an accessible name or label.");
        }
    }

    [TestMethod]
    public void PageReferenceAndReadOnlySyncOutputsHaveResolvedAccessibleLabels()
    {
        string viewsDirectory = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CortexCompanion",
            "Views");
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        XDocument pagesDocument = XDocument.Load(Path.Combine(viewsDirectory, "PagesView.xaml"));
        XElement pageReference = pagesDocument
            .Descendants()
            .Single(element =>
                element.Name.LocalName == "TextBox" &&
                element.Attribute("Text")?.Value.Contains("PageReference", StringComparison.Ordinal) == true);

        Assert.AreEqual(
            "{Binding ElementName=PageReferenceLabel}",
            pageReference.Attribute("AutomationProperties.LabeledBy")?.Value);
        Assert.IsNotNull(pagesDocument
            .Descendants()
            .SingleOrDefault(element => element.Attribute(xaml + "Name")?.Value == "PageReferenceLabel"));

        XDocument syncDocument = XDocument.Load(Path.Combine(viewsDirectory, "SyncView.xaml"));
        Dictionary<string, string> expectedLabels = new(StringComparer.Ordinal)
        {
            ["StandardError"] = "StandardErrorLabel",
            ["StandardOutput"] = "StandardOutputLabel",
        };
        XElement[] readOnlyOutputs = syncDocument
            .Descendants()
            .Where(element =>
                element.Name.LocalName == "TextBox" &&
                element.Attribute("IsReadOnly")?.Value == "True")
            .ToArray();

        Assert.HasCount(expectedLabels.Count, readOnlyOutputs);
        foreach (KeyValuePair<string, string> expectedLabel in expectedLabels)
        {
            XElement output = readOnlyOutputs.Single(element =>
                element.Attribute("Text")?.Value.Contains(expectedLabel.Key, StringComparison.Ordinal) == true);
            Assert.AreEqual(
                $"{{Binding ElementName={expectedLabel.Value}}}",
                output.Attribute("AutomationProperties.LabeledBy")?.Value);
            Assert.IsNotNull(syncDocument
                .Descendants()
                .SingleOrDefault(element => element.Attribute(xaml + "Name")?.Value == expectedLabel.Value));
        }
    }

    [TestMethod]
    public void FrenchUserFacingResourcesDoNotRegressToReviewedAsciiSpellings()
    {
        XDocument document = XDocument.Load(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CortexCompanion",
            "Localization",
            "UiStrings.resx"));
        Dictionary<string, string> values = document
            .Descendants("data")
            .ToDictionary(
                element => element.Attribute("name")?.Value ?? string.Empty,
                element => element.Element("value")?.Value ?? string.Empty,
                StringComparer.Ordinal);

        foreach (KeyValuePair<string, string> value in values)
        {
            Match missingDiacritic = MissingFrenchDiacriticRegex().Match(value.Value);
            Assert.IsFalse(
                missingDiacritic.Success,
                $"{value.Key} still contains the reviewed ASCII spelling '{missingDiacritic.Value}'.");
        }

        Dictionary<string, string> reviewedContextualSpellings =
            new(StringComparer.Ordinal)
            {
                ["PagesNoSpaces"] = "configuré",
                ["PagesCliError"] = "refusé",
                ["WholeSpaceModeDescription"] = "collecté",
                ["SyncStateReady"] = "rechargé",
                ["SyncNeverRun"] = "synchronisé",
                ["PatExpired"] = "Expiré",
                ["SyncAuthFailed"] = "expiré ou refusé",
                ["SyncUnexpectedExit"] = "a retourné",
                ["CredentialStored"] = "enregistré",
            };

        foreach (KeyValuePair<string, string> expectedSpelling in reviewedContextualSpellings)
        {
            Assert.Contains(expectedSpelling.Value, values[expectedSpelling.Key], StringComparison.Ordinal);
        }
    }

    [TestMethod]
    public void MainNavigationExposesSelectionAndKeepsItInSyncWithTheVisiblePage()
    {
        XDocument document = XDocument.Load(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CortexCompanion",
            "MainWindow.xaml"));
        XElement[] navigationItems = document
            .Descendants()
            .Where(element => element.Name.LocalName == "RadioButton")
            .ToArray();

        Assert.HasCount(4, navigationItems);
        foreach (XElement navigationItem in navigationItems)
        {
            XAttribute? checkedBinding = navigationItem.Attribute("IsChecked");
            Assert.IsNotNull(checkedBinding);
            StringAssert.Contains(checkedBinding.Value, "Mode=TwoWay");
            Assert.IsNotNull(navigationItem.Attribute("AutomationProperties.Name"));
        }
    }

    [TestMethod]
    public void InformationalVersionIsOneExactCalVerToken()
    {
        string version = CompanionVersionProvider.GetCurrent();

        Assert.IsTrue(CalVerRegex().IsMatch(version));
        Assert.IsFalse(version.Contains('+', StringComparison.Ordinal));
    }

    [TestMethod]
    public void VersionCommandIsHandledBeforeWpfConstruction()
    {
        string source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CortexCompanion",
            "Program.cs"));

        int versionWriteIndex = source.IndexOf("VersionOutputWriter.TryWriteLine", StringComparison.Ordinal);
        int applicationConstructionIndex = source.IndexOf("App application = new();", StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, versionWriteIndex);
        Assert.IsGreaterThan(versionWriteIndex, applicationConstructionIndex);
    }

    [TestMethod]
    public void UninstallCleanupIsHandledBeforeWpfConstructionWithoutChangingVersionMode()
    {
        string source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CortexCompanion",
            "Program.cs"));

        int versionIndex = source.IndexOf("CompanionVersionArgument", StringComparison.Ordinal);
        int cleanupIndex = source.IndexOf("CompanionUninstallCleanupArgument", StringComparison.Ordinal);
        int applicationIndex = source.IndexOf("App application = new();", StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, versionIndex);
        Assert.IsGreaterThan(versionIndex, cleanupIndex);
        Assert.IsGreaterThan(cleanupIndex, applicationIndex);
    }

    [TestMethod]
    public void PublishContractFailsClosedAndNamesEveryRedistributionSidecar()
    {
        string project = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CortexCompanion",
            "CortexCompanion.csproj"));
        string[] expectedNames =
        [
            "LICENSE.txt",
            "ThirdPartyNotices.txt",
            "WPF-LICENSE.txt",
            "WPF-ThirdPartyNotices.txt",
            "Tomlyn-LICENSE.txt",
            "CortexCompanion-LICENSE.txt",
        ];

        foreach (string expectedName in expectedNames)
        {
            Assert.Contains(expectedName, project, StringComparison.Ordinal);
        }

        Assert.Contains("<Error Condition=\"!Exists(", project, StringComparison.Ordinal);
        Assert.Contains("ExcludeFromSingleFile=\"true\"", project, StringComparison.Ordinal);
    }

    [TestMethod]
    public void DarkThemeExplicitlyColorsExpanderHeadersAndLoadingProgress()
    {
        string controls = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CortexCompanion",
            "Themes",
            "CommonControls.xaml"));

        Assert.Contains("<Style TargetType=\"Expander\">", controls, StringComparison.Ordinal);
        Assert.Contains(
            "Property=\"Foreground\" Value=\"{DynamicResource TextPrimaryBrush}\"",
            controls,
            StringComparison.Ordinal);
        Assert.Contains("<Style TargetType=\"ProgressBar\">", controls, StringComparison.Ordinal);
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

    [GeneratedRegex(@"^\d{4}\.\d{4}\.\d{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex CalVerRegex();

    [GeneratedRegex(
        @"\b(?:ajoutee|annulee|cle|confirmee|conserves|degrade|delai|demarrage|depasse|desactivees|detectee|derniere|demandee|echeance|echec|echoue|enregistree|etat|etait|etre|generation|numerique|operation|purgee|reference|refusee|repondu|resoudre|resolution|retiree|separement|succes|terminee|typee|validite|verification|verrouille|verrouilles)\b",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex MissingFrenchDiacriticRegex();
}
