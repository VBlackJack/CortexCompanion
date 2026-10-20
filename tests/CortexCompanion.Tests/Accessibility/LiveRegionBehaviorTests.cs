// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;
using CortexCompanion.Accessibility;

namespace CortexCompanion.Tests.Accessibility;

/// <summary>Guards the explicit UI Automation notification path for bound live regions.</summary>
[TestClass]
public sealed class LiveRegionBehaviorTests
{
    [STATestMethod]
    public void EveryDistinctAnnouncementRaisesOneAutomationNotificationRequest()
    {
        TextBlock region = new();
        List<UIElement> raisedFor = [];
        LiveRegionBehavior.EventRaiser = raisedFor.Add;
        try
        {
            LiveRegionBehavior.SetAnnouncement(region, "starting");
            LiveRegionBehavior.SetAnnouncement(region, "starting");
            LiveRegionBehavior.SetAnnouncement(region, "completed");

            Assert.HasCount(2, raisedFor);
            Assert.IsTrue(raisedFor.All(element => ReferenceEquals(element, region)));
        }
        finally
        {
            LiveRegionBehavior.ResetEventRaiser();
        }
    }

    [TestMethod]
    public void EveryDeclaredLiveRegionBindsTheExplicitAnnouncementBehavior()
    {
        string sourceRoot = Path.Combine(FindRepositoryRoot(), "src", "CortexCompanion");
        string[] paths = Directory.EnumerateFiles(Path.Combine(sourceRoot, "Views"), "*.xaml")
            .Append(Path.Combine(sourceRoot, "MainWindow.xaml"))
            .ToArray();
        HashSet<string> filesWithLiveRegions = new(StringComparer.Ordinal);
        List<string> silent = [];

        foreach (string path in paths)
        {
            string fileName = Path.GetFileName(path);
            foreach (XElement liveRegion in FindLiveRegions(XDocument.Load(path, LoadOptions.SetLineInfo)))
            {
                filesWithLiveRegions.Add(fileName);
                if (!liveRegion.Attributes().Any(attribute =>
                        attribute.Name.LocalName.EndsWith("Announcement", StringComparison.Ordinal)))
                {
                    silent.Add($"{fileName}: {liveRegion.Name.LocalName} line {((IXmlLineInfo)liveRegion).LineNumber}");
                }
            }
        }

        // The scan must reach the views that own outcome messages, or an empty result proves nothing.
        foreach (string expected in new[] { "MainWindow.xaml", "SettingsView.xaml", "SyncView.xaml", "SchedulingView.xaml", "PagesView.xaml", "SourceSelectionDialog.xaml" })
        {
            Assert.Contains(expected, filesWithLiveRegions, $"{expected} declares no live region.");
        }

        Assert.IsEmpty(
            silent,
            $"These live regions have no explicit UIA notification binding: {string.Join(", ", silent)}.");
    }

    private static HashSet<XElement> FindLiveRegions(XDocument document)
    {
        HashSet<XElement> regions = [];
        foreach (XElement element in document.Descendants())
        {
            if (element.Attributes().Any(attribute =>
                    attribute.Name.LocalName.EndsWith("LiveSetting", StringComparison.Ordinal)))
            {
                regions.Add(element);
            }

            // A politeness chosen by a style setter belongs to the element that owns the style.
            if (element.Name.LocalName == "Setter" &&
                ((string?)element.Attribute("Property"))?.EndsWith("LiveSetting", StringComparison.Ordinal) == true)
            {
                XElement? owner = element.Ancestors().FirstOrDefault(ancestor =>
                    !ancestor.Name.LocalName.Contains('.', StringComparison.Ordinal) &&
                    ancestor.Name.LocalName is not ("Style" or "Setter" or "Trigger" or "DataTrigger" or "MultiDataTrigger"));
                if (owner is not null)
                {
                    regions.Add(owner);
                }
            }
        }

        return regions;
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
