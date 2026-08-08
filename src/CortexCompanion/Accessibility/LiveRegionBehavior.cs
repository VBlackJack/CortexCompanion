// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows;
using System.Windows.Automation.Peers;

namespace CortexCompanion.Accessibility;

/// <summary>Raises the UI Automation event that makes bound live-region updates observable.</summary>
public static class LiveRegionBehavior
{
    /// <summary>Identifies the bound announcement whose changes raise a live-region event.</summary>
    public static readonly DependencyProperty AnnouncementProperty = DependencyProperty.RegisterAttached(
        "Announcement",
        typeof(string),
        typeof(LiveRegionBehavior),
        new PropertyMetadata(string.Empty, AnnouncementChanged));

    internal static Action<UIElement> EventRaiser { get; set; } = RaiseLiveRegionChanged;

    /// <summary>Sets the announcement value observed for changes.</summary>
    public static void SetAnnouncement(DependencyObject element, string value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(AnnouncementProperty, value);
    }

    /// <summary>Gets the announcement value observed for changes.</summary>
    public static string GetAnnouncement(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(AnnouncementProperty) as string ?? string.Empty;
    }

    internal static void ResetEventRaiser() => EventRaiser = RaiseLiveRegionChanged;

    private static void AnnouncementChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject is UIElement element &&
            !Equals(eventArgs.OldValue, eventArgs.NewValue))
        {
            EventRaiser(element);
        }
    }

    private static void RaiseLiveRegionChanged(UIElement element)
    {
        AutomationPeer? peer = UIElementAutomationPeer.FromElement(element) ??
            UIElementAutomationPeer.CreatePeerForElement(element);
        peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }
}
