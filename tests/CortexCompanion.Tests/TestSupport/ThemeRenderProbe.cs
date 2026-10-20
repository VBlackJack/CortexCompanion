// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Calendar = System.Windows.Controls.Calendar;

namespace CortexCompanion.Tests.TestSupport;

/// <summary>Measures what the dark theme actually renders, where a XAML scan cannot see.</summary>
/// <remarks>
/// Built-in WPF templates carry their own colours and focus visuals. These probes render
/// controls under the running application's resources, so they run inside the one
/// Application the test process may host: a window created after that Application has shut
/// down was observed to render without its built-in templates.
/// </remarks>
internal static class ThemeRenderProbe
{
    private const double MinimumContrastRatio = 4.5;
    private const double OffscreenCoordinate = -10000;
    private const double ProbeWindowWidth = 480;
    private const double ProbeWindowHeight = 900;
    private const int MinimumMeasuredTexts = 40;
    private static readonly DateTime FixtureDate = new(2026, 10, 20, 0, 0, 0, DateTimeKind.Local);

    /// <summary>Returns every calendar or date picker text that misses the contrast floor.</summary>
    /// <remarks>
    /// Measured before the theme templated these parts: day numbers, day titles, month
    /// buttons and the header rendered #FF333333 on the #FF21222C surface (1.25:1), inactive
    /// days #FF777777 (3.53:1), and the date text #FFF8F8F2 on a white field (1.07:1).
    /// </remarks>
    public static IReadOnlyList<string> FindUnreadableCalendarText(Window owner)
    {
        Calendar calendar = new() { DisplayDate = FixtureDate, SelectedDate = FixtureDate };
        Calendar yearCalendar = new() { DisplayDate = FixtureDate, DisplayMode = CalendarMode.Year };
        DatePicker picker = new() { SelectedDate = FixtureDate };
        StackPanel panel = new();
        panel.Children.Add(picker);
        panel.Children.Add(calendar);
        panel.Children.Add(yearCalendar);
        Window window = CreateProbeWindow(owner, panel);
        List<string> failures = [];

        try
        {
            window.Show();
            window.UpdateLayout();

            int measured = MeasureTexts(calendar, failures) + MeasureTexts(yearCalendar, failures);
            Assert.IsTrue(Descendants(calendar).OfType<CalendarDayButton>().Any(), "The month view rendered no day buttons.");
            Assert.IsTrue(Descendants(yearCalendar).OfType<CalendarButton>().Any(), "The year view rendered no month buttons.");
            Assert.IsGreaterThan(MinimumMeasuredTexts, measured, "Too few calendar texts were measured to prove anything.");

            DatePickerTextBox pickerText = Descendants(picker).OfType<DatePickerTextBox>().Single();
            AddFailureIfBelow(
                failures,
                nameof(DatePickerTextBox),
                SolidColor(pickerText.Foreground),
                EffectiveBackground(pickerText, includeSelf: true));

            // The calendar a user actually meets is the one inside the date picker popup.
            picker.IsDropDownOpen = true;
            window.UpdateLayout();
            Popup popup = (Popup)picker.Template.FindName("PART_Popup", picker);
            Assert.IsNotNull(popup.Child, "The date picker popup has no calendar.");
            popup.Child.UpdateLayout();
            Assert.IsGreaterThan(MinimumMeasuredTexts, MeasureTexts(popup.Child, failures), "Too few popup calendar texts were measured.");
            picker.IsDropDownOpen = false;
        }
        finally
        {
            window.Close();
        }

        return failures;
    }

    /// <summary>Returns every themed control whose keyboard focus does not draw the app focus ring.</summary>
    public static IReadOnlyList<string> FindControlsWithoutAppFocusVisual(Window owner)
    {
        RadioButton radio = new() { Content = nameof(RadioButton) };
        CheckBox check = new() { Content = nameof(CheckBox) };
        Expander expander = new() { Header = nameof(Expander) };
        GridSplitter splitter = new() { Width = ProbeWindowWidth / 2 };
        ListBox list = new();
        list.Items.Add(nameof(ListBoxItem));
        TreeView tree = new() { ItemContainerStyle = (Style)owner.FindResource("SourceTreeItemStyle") };
        tree.Items.Add(nameof(TreeViewItem));
        StackPanel panel = new();
        foreach (UIElement element in new UIElement[] { radio, check, expander, splitter, list, tree })
        {
            panel.Children.Add(element);
        }

        Window window = CreateProbeWindow(owner, panel);
        List<string> missing = [];
        try
        {
            window.Show();
            window.UpdateLayout();
            Style expected = (Style)owner.FindResource("AppFocusVisualStyle");
            ToggleButton expanderHeader = Descendants(expander).OfType<ToggleButton>().Single();
            ListBoxItem listItem = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(0);
            TreeViewItem treeItem = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromIndex(0);
            Dictionary<string, FrameworkElement> probes = new(StringComparer.Ordinal)
            {
                [nameof(RadioButton)] = radio,
                [nameof(CheckBox)] = check,
                ["Expander header"] = expanderHeader,
                [nameof(GridSplitter)] = splitter,
                [nameof(ListBoxItem)] = listItem,
                [nameof(TreeViewItem)] = treeItem,
            };

            foreach (KeyValuePair<string, FrameworkElement> probe in probes)
            {
                if (!ReferenceEquals(expected, probe.Value.FocusVisualStyle))
                {
                    missing.Add(probe.Key);
                }
            }
        }
        finally
        {
            window.Close();
        }

        return missing;
    }

    /// <summary>Returns every list item text that misses the floor, selected (active and inactive) or at rest.</summary>
    public static IReadOnlyList<string> FindUnreadableSelectedListText(Window owner)
    {
        // Mirrors the search results list, which paints the page background behind its items.
        ListBox list = new() { Width = ProbeWindowWidth, Background = (Brush)owner.FindResource("BackgroundBrush") };
        list.Items.Add(new TextBlock { Text = nameof(ListBoxItem), Foreground = (Brush)owner.FindResource("TextPrimaryBrush") });
        list.Items.Add(new TextBlock { Text = nameof(ListBoxItem), Foreground = (Brush)owner.FindResource("TextSecondaryBrush") });
        TextBox focusThief = new();
        StackPanel panel = new();
        panel.Children.Add(list);
        panel.Children.Add(focusThief);
        Window window = CreateProbeWindow(owner, panel);
        List<string> failures = [];
        try
        {
            window.Show();
            list.SelectedIndex = 0;
            window.UpdateLayout();
            MeasureTexts(list, failures);
            list.SelectedIndex = 1;
            focusThief.Focus();
            window.UpdateLayout();
            MeasureTexts(list, failures);
        }
        finally
        {
            window.Close();
        }

        return failures;
    }

    private static Window CreateProbeWindow(Window owner, UIElement content) => new()
    {
        Owner = owner,
        Content = content,
        ShowInTaskbar = false,
        ShowActivated = false,
        WindowStartupLocation = WindowStartupLocation.Manual,
        Left = OffscreenCoordinate,
        Top = OffscreenCoordinate,
        Width = ProbeWindowWidth,
        Height = ProbeWindowHeight,
    };

    private static int MeasureTexts(DependencyObject root, List<string> failures)
    {
        int measured = 0;
        foreach (TextBlock text in Descendants(root).OfType<TextBlock>())
        {
            if (string.IsNullOrWhiteSpace(text.Text) || !text.IsVisible)
            {
                continue;
            }

            measured++;
            string owner = Ancestors(text).FirstOrDefault(ancestor => ancestor is Control)?.GetType().Name ?? nameof(TextBlock);
            AddFailureIfBelow(failures, $"{owner} '{text.Text}'", SolidColor(text.Foreground), EffectiveBackground(text, includeSelf: false));
        }

        return measured;
    }

    private static void AddFailureIfBelow(List<string> failures, string subject, Color foreground, Color background)
    {
        double ratio = CalculateContrastRatio(foreground, background);
        if (ratio < MinimumContrastRatio)
        {
            failures.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"{subject}: {foreground} on {background} is {ratio:F2}:1."));
        }
    }

    private static Color EffectiveBackground(DependencyObject element, bool includeSelf)
    {
        IEnumerable<DependencyObject> chain = includeSelf ? Ancestors(element).Prepend(element) : Ancestors(element);
        foreach (DependencyObject current in chain)
        {
            Brush? brush = current switch
            {
                Border border => border.Background,
                Panel panel => panel.Background,
                TextBoxBase textBox => textBox.Background,
                Window window => window.Background,
                _ => null,
            };
            if (brush is not null && TryOpaqueColor(brush, out Color color))
            {
                return color;
            }
        }

        return Colors.White;
    }

    private static bool TryOpaqueColor(Brush brush, out Color color)
    {
        color = default;
        if (brush.Opacity <= 0)
        {
            return false;
        }

        switch (brush)
        {
            case SolidColorBrush solid when solid.Color.A > 0:
                color = solid.Color;
                return true;
            case GradientBrush gradient when gradient.GradientStops.Count > 0:
                color = gradient.GradientStops[0].Color;
                return color.A > 0;
            default:
                return false;
        }
    }

    private static Color SolidColor(Brush brush) => brush switch
    {
        SolidColorBrush solid => solid.Color,
        GradientBrush gradient when gradient.GradientStops.Count > 0 => gradient.GradientStops[0].Color,
        _ => Colors.Black,
    };

    private static IEnumerable<DependencyObject> Ancestors(DependencyObject element)
    {
        DependencyObject? current = VisualTreeHelper.GetParent(element);
        while (current is not null)
        {
            yield return current;
            current = VisualTreeHelper.GetParent(current);
        }
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, index);
            yield return child;
            foreach (DependencyObject descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    private static double CalculateContrastRatio(Color foreground, Color background)
    {
        double foregroundLuminance = CalculateRelativeLuminance(foreground);
        double backgroundLuminance = CalculateRelativeLuminance(background);
        double lighter = Math.Max(foregroundLuminance, backgroundLuminance);
        double darker = Math.Min(foregroundLuminance, backgroundLuminance);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double CalculateRelativeLuminance(Color color) =>
        (0.2126 * ConvertSrgbChannel(color.R)) +
        (0.7152 * ConvertSrgbChannel(color.G)) +
        (0.0722 * ConvertSrgbChannel(color.B));

    private static double ConvertSrgbChannel(byte channel)
    {
        double value = channel / 255.0;
        return value <= 0.04045
            ? value / 12.92
            : Math.Pow((value + 0.055) / 1.055, 2.4);
    }
}
