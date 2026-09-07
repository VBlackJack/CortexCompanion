// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Text.Json;
using CortexCompanion.Constants;

namespace CortexCompanion.Services;

/// <summary>Selects the interface language before any localized string is read.</summary>
/// <remarks>
/// This deliberately reads the settings file itself instead of going through
/// <see cref="SettingsStore"/>. Several UiStrings members are static CompositeFormat
/// values parsed once when the type is first touched, so the culture has to be in place
/// before anything reaches that type. Loading settings the ordinary way would drag the
/// logger and its messages in first and freeze half the interface in the wrong language.
/// A change therefore applies at the next start, which is what the settings screen says.
/// </remarks>
public static class UiCultureBootstrap
{
    /// <summary>The settings property naming the interface language, in camel case.</summary>
    private const string LanguagePropertyName = "uiLanguage";

    /// <summary>Applies the stored language, leaving the Windows language when none is stored.</summary>
    /// <param name="settingsPath">The application settings file, which need not exist.</param>
    /// <returns>The applied language name, or null when Windows decides.</returns>
    public static string? Apply(string settingsPath)
    {
        string? language = Read(settingsPath);
        if (language is null)
        {
            return null;
        }

        CultureInfo culture = new(language);
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentUICulture = culture;
        return language;
    }

    /// <summary>Reads the stored language, answering null for every unusable file.</summary>
    /// <remarks>
    /// A missing, locked, truncated or hand-edited settings file must never stop the
    /// application from starting; it only means Windows keeps deciding the language.
    /// </remarks>
    public static string? Read(string settingsPath)
    {
        if (string.IsNullOrWhiteSpace(settingsPath) || !File.Exists(settingsPath))
        {
            return null;
        }

        try
        {
            using FileStream stream = new(settingsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using JsonDocument document = JsonDocument.Parse(stream);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                if (!string.Equals(property.Name, LanguagePropertyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return property.Value.ValueKind == JsonValueKind.String
                    ? AppConstants.NormalizeUiLanguage(property.Value.GetString())
                    : null;
            }

            return null;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            return null;
        }
    }
}
