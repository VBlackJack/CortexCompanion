// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using CortexCompanion.Constants;
using CortexCompanion.Localization;

namespace CortexCompanion.Models;

/// <summary>One interface language offered in the settings screen.</summary>
/// <param name="Culture">The culture name, or null to follow the Windows language.</param>
/// <param name="Label">The text shown in the list.</param>
public sealed record UiLanguageOption(string? Culture, string Label)
{
    /// <summary>Builds the offered languages, the Windows default first.</summary>
    /// <remarks>
    /// Each language is named in its own language, which is what a reader looking for their
    /// own language expects to find, and which spares the set from being translated again in
    /// every satellite. The names come from the framework rather than from a literal.
    /// </remarks>
    public static IReadOnlyList<UiLanguageOption> Build() =>
    [
        new(null, UiStrings.SettingsLanguageSystem),
        .. AppConstants.SupportedUiLanguages.Select(
            name => new UiLanguageOption(name, NativeName(name))),
    ];

    /// <summary>Returns the language's own name, capitalized as a list entry.</summary>
    private static string NativeName(string name)
    {
        CultureInfo culture = CultureInfo.GetCultureInfo(name);
        string native = culture.NativeName;
        return native.Length == 0
            ? name
            : culture.TextInfo.ToUpper(native[0]) + native[1..];
    }

    /// <summary>Returns the option matching one stored language, or the Windows default.</summary>
    public static UiLanguageOption Resolve(IReadOnlyList<UiLanguageOption> options, string? culture) =>
        options.FirstOrDefault(option => string.Equals(option.Culture, culture, StringComparison.OrdinalIgnoreCase))
        ?? options[0];
}
