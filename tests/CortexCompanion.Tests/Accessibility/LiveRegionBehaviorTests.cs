// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows;
using System.Windows.Controls;
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
        string root = FindRepositoryRoot();
        string[] relativePaths =
        [
            Path.Combine("src", "CortexCompanion", "MainWindow.xaml"),
            Path.Combine("src", "CortexCompanion", "Views", "SettingsView.xaml"),
            Path.Combine("src", "CortexCompanion", "Views", "SyncView.xaml"),
        ];

        foreach (string relativePath in relativePaths)
        {
            XDocument document = XDocument.Load(Path.Combine(root, relativePath));
            XElement[] liveRegions = document.Descendants()
                .Where(element => element.Attributes().Any(attribute =>
                    attribute.Name.LocalName.EndsWith("LiveSetting", StringComparison.Ordinal)))
                .ToArray();

            Assert.IsNotEmpty(liveRegions, relativePath);
            foreach (XElement liveRegion in liveRegions)
            {
                Assert.IsTrue(
                    liveRegion.Attributes().Any(attribute =>
                        attribute.Name.LocalName.EndsWith("Announcement", StringComparison.Ordinal)),
                    $"{relativePath} contains a live region without an explicit UIA notification binding.");
            }
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
