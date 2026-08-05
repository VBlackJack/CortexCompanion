// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml.Linq;

namespace CortexCompanion.Tests.Themes;

/// <summary>Guards theme contrast and the resource wiring that selects interactive state colors.</summary>
[TestClass]
public sealed class ThemeContrastTests
{
    private const double MinimumContrastRatio = 4.5;
    private static readonly Regex HexColorPattern = new("#[0-9A-Fa-f]{6,8}", RegexOptions.CultureInvariant);

    private static readonly ThemeContrastCase[] ContrastCases =
    [
        new("Primary button rest", "BackgroundBrush", "AccentBrush"),
        new("Primary button hover", "BackgroundBrush", "AccentHoverBrush"),
        new("Primary button pressed", "BackgroundBrush", "AccentPressedBrush"),
        new("Navigation button rest", "TextPrimaryBrush", "SurfaceBrush"),
        new("Navigation button hover", "TextPrimaryBrush", "HighlightBrush"),
        new("Navigation button pressed", "TextPrimaryBrush", "CardBrush"),
        new("Secondary button rest", "TextPrimaryBrush", "CardBrush"),
        new("Secondary button hover", "TextPrimaryBrush", "HighlightBrush"),
        new("Secondary button pressed", "TextPrimaryBrush", "CardBrush"),
        new("Text box rest", "TextPrimaryBrush", "SurfaceBrush"),
        new("Text box selection", "BackgroundBrush", "AccentBrush"),
        new("Combo box rest", "TextPrimaryBrush", "SurfaceBrush"),
        new("Combo item rest", "TextPrimaryBrush", "SurfaceBrush"),
        new("Combo item hover", "BackgroundBrush", "AccentHoverBrush"),
        new("Combo item selected", "BackgroundBrush", "AccentBrush"),
        new("ToolTip rest", "TextPrimaryBrush", "CardBrush"),
    ];

    /// <summary>Ensures every interactive text and background pair meets WCAG AA.</summary>
    [TestMethod]
    public void InteractiveThemeStatesMeetMinimumContrast()
    {
        RunInSta(() =>
        {
            ResourceDictionary theme = LoadTheme();

            foreach (ThemeContrastCase contrastCase in ContrastCases)
            {
                Color foreground = GetBrush(theme, contrastCase.ForegroundKey).Color;
                Color background = GetBrush(theme, contrastCase.BackgroundKey).Color;
                double ratio = CalculateContrastRatio(foreground, background);

                Assert.IsGreaterThanOrEqualTo(
                    MinimumContrastRatio,
                    ratio,
                    $"{contrastCase.Name}: {contrastCase.ForegroundKey}/{contrastCase.BackgroundKey} " +
                    $"has contrast {ratio:F4}:1, below {MinimumContrastRatio:F1}:1.");
            }
        });
    }

    /// <summary>Ensures the primary and orphan-control styles use the measured theme brushes.</summary>
    [TestMethod]
    public void InteractiveStylesUseGuardedBrushes()
    {
        XDocument commonControls = XDocument.Load(GetCommonControlsPath());
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

        XElement primaryStyle = FindStyle(commonControls, presentation, xaml, "PrimaryButtonStyle");
        AssertSetterValue(primaryStyle, presentation, "Foreground", "{DynamicResource BackgroundBrush}");
        AssertTriggerBackground(primaryStyle, presentation, "IsMouseOver", "{DynamicResource AccentHoverBrush}");
        AssertTriggerBackground(primaryStyle, presentation, "IsPressed", "{DynamicResource AccentPressedBrush}");
        AssertTriggerSetterValue(
            primaryStyle,
            presentation,
            "IsMouseOver",
            "Foreground",
            "{DynamicResource BackgroundBrush}");
        AssertTriggerSetterValue(
            primaryStyle,
            presentation,
            "IsPressed",
            "Foreground",
            "{DynamicResource BackgroundBrush}");

        XElement textBoxStyle = FindImplicitStyle(commonControls, presentation, "TextBox");
        AssertSetterValue(textBoxStyle, presentation, "SelectionBrush", "{DynamicResource AccentBrush}");
        AssertSetterValue(textBoxStyle, presentation, "SelectionTextBrush", "{DynamicResource BackgroundBrush}");

        XElement comboItemStyle = FindImplicitStyle(commonControls, presentation, "ComboBoxItem");
        AssertTriggerBackground(comboItemStyle, presentation, "IsHighlighted", "{DynamicResource AccentHoverBrush}");
        AssertTriggerBackground(comboItemStyle, presentation, "IsSelected", "{DynamicResource AccentBrush}");
        AssertTriggerSetterValue(
            comboItemStyle,
            presentation,
            "IsHighlighted",
            "Foreground",
            "{DynamicResource BackgroundBrush}");
        AssertTriggerSetterValue(
            comboItemStyle,
            presentation,
            "IsSelected",
            "Foreground",
            "{DynamicResource BackgroundBrush}");

        XElement comboStyle = FindImplicitStyle(commonControls, presentation, "ComboBox");
        XElement selectionPresenter = comboStyle.Descendants(presentation + "ContentPresenter")
            .Single(element => string.Equals(
                (string?)element.Attribute("Content"),
                "{TemplateBinding SelectionBoxItem}",
                StringComparison.Ordinal));
        Assert.AreEqual(
            "{TemplateBinding ItemTemplateSelector}",
            (string?)selectionPresenter.Attribute("ContentTemplateSelector"));
    }

    /// <summary>Ensures color literals stay centralized in the palette dictionary.</summary>
    [TestMethod]
    public void ThemeAndViewColorLiteralsAreCentralized()
    {
        string repositoryRoot = FindRepositoryRoot();
        string themeDirectory = Path.Combine(repositoryRoot, "src", "CortexCompanion", "Themes");
        string viewDirectory = Path.Combine(repositoryRoot, "src", "CortexCompanion", "Views");
        string mainWindowPath = Path.Combine(repositoryRoot, "src", "CortexCompanion", "MainWindow.xaml");
        string darkThemePath = Path.Combine(themeDirectory, "DarkTheme.xaml");
        IEnumerable<string> paths = Directory.EnumerateFiles(themeDirectory, "*.xaml")
            .Concat(Directory.EnumerateFiles(viewDirectory, "*.xaml"))
            .Append(mainWindowPath)
            .Where(path => !string.Equals(path, darkThemePath, StringComparison.OrdinalIgnoreCase));

        foreach (string path in paths)
        {
            Match match = HexColorPattern.Match(File.ReadAllText(path));
            Assert.IsFalse(match.Success, $"Hex color literal {match.Value} found in {path}.");
        }
    }

    private static ResourceDictionary LoadTheme()
    {
        string themePath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CortexCompanion",
            "Themes",
            "DarkTheme.xaml");
        ParserContext parserContext = new()
        {
            BaseUri = new Uri(themePath, UriKind.Absolute),
        };

        using FileStream stream = File.OpenRead(themePath);
        object loaded = XamlReader.Load(stream, parserContext);
        return loaded as ResourceDictionary
            ?? throw new InvalidOperationException("DarkTheme.xaml did not load as a ResourceDictionary.");
    }

    private static SolidColorBrush GetBrush(ResourceDictionary theme, string key) =>
        theme[key] as SolidColorBrush
        ?? throw new InvalidOperationException($"Theme resource {key} is not a SolidColorBrush.");

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

    private static XElement FindStyle(
        XDocument document,
        XNamespace presentation,
        XNamespace xaml,
        string key) =>
        document.Descendants(presentation + "Style")
            .Single(element => string.Equals((string?)element.Attribute(xaml + "Key"), key, StringComparison.Ordinal));

    private static XElement FindImplicitStyle(
        XDocument document,
        XNamespace presentation,
        string targetType) =>
        document.Descendants(presentation + "Style")
            .Single(element =>
                element.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml")) is null &&
                string.Equals((string?)element.Attribute("TargetType"), targetType, StringComparison.Ordinal));

    private static void AssertTriggerBackground(
        XElement style,
        XNamespace presentation,
        string triggerProperty,
        string expectedValue) =>
        AssertTriggerSetterValue(style, presentation, triggerProperty, "Background", expectedValue);

    private static void AssertTriggerSetterValue(
        XElement style,
        XNamespace presentation,
        string triggerProperty,
        string setterProperty,
        string expectedValue)
    {
        XElement trigger = style.Descendants(presentation + "Trigger")
            .Single(element => string.Equals((string?)element.Attribute("Property"), triggerProperty, StringComparison.Ordinal));
        XElement setter = trigger.Elements(presentation + "Setter")
            .Single(element => string.Equals((string?)element.Attribute("Property"), setterProperty, StringComparison.Ordinal));

        Assert.AreEqual(expectedValue, (string?)setter.Attribute("Value"));
    }

    private static void AssertSetterValue(
        XElement style,
        XNamespace presentation,
        string property,
        string expectedValue)
    {
        XElement setter = style.Elements(presentation + "Setter")
            .Single(element => string.Equals((string?)element.Attribute("Property"), property, StringComparison.Ordinal));

        Assert.AreEqual(expectedValue, (string?)setter.Attribute("Value"));
    }

    private static string GetCommonControlsPath() => Path.Combine(
        FindRepositoryRoot(),
        "src",
        "CortexCompanion",
        "Themes",
        "CommonControls.xaml");

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

    private static void RunInSta(Action action)
    {
        Exception? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    private sealed record ThemeContrastCase(string Name, string ForegroundKey, string BackgroundKey);
}
