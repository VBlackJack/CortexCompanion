// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

/// <summary>Guards the language selection that runs before any localized string is read.</summary>
[TestClass]
public sealed class UiCultureBootstrapTests
{
    [TestMethod]
    public void AnAbsentSettingsFileLeavesWindowsToDecide()
    {
        using TemporaryDirectory temporary = new();

        Assert.IsNull(UiCultureBootstrap.Read(Path.Combine(temporary.Path, "settings.json")));
    }

    [TestMethod]
    public void AStoredSupportedLanguageIsRead()
    {
        Assert.AreEqual("fr", ReadFrom("""{"cliPath":"c:/cortex.exe","uiLanguage":"fr"}"""));
    }

    [TestMethod]
    public void TheStoredNameIsMatchedWithoutRegardToCase()
    {
        Assert.AreEqual("en", ReadFrom("""{"UiLanguage":"EN"}"""));
    }

    [TestMethod]
    [DataRow("""{"uiLanguage":"de"}""", DisplayName = "a language Companion does not ship")]
    [DataRow("""{"uiLanguage":""}""", DisplayName = "an empty name")]
    [DataRow("""{"uiLanguage":null}""", DisplayName = "an explicit null")]
    [DataRow("""{"uiLanguage":42}""", DisplayName = "a number where a name belongs")]
    [DataRow("""{"cliPath":"c:/cortex.exe"}""", DisplayName = "a settings file predating the setting")]
    [DataRow("""{"uiLanguage":"fr" """, DisplayName = "a truncated document")]
    [DataRow("[]", DisplayName = "an array where the settings object belongs")]
    [DataRow("", DisplayName = "an empty file")]
    public void AnUnusableValueLeavesWindowsToDecide(string content)
    {
        // A hand-edited or half-written settings file must never stop the application from
        // starting; it only means the language keeps following Windows.
        Assert.IsNull(ReadFrom(content));
    }

    [TestMethod]
    public void ApplyingAStoredLanguageChangesTheCultureStringsAreReadIn()
    {
        CultureInfo previousDefault = CultureInfo.DefaultThreadCurrentUICulture ?? CultureInfo.CurrentUICulture;
        CultureInfo previousCurrent = CultureInfo.CurrentUICulture;
        try
        {
            using TemporaryDirectory temporary = new();
            string settingsPath = Path.Combine(temporary.Path, "settings.json");
            File.WriteAllText(settingsPath, """{"uiLanguage":"fr"}""");

            string? applied = UiCultureBootstrap.Apply(settingsPath);

            Assert.AreEqual("fr", applied);
            Assert.AreEqual("fr", CultureInfo.CurrentUICulture.Name);
        }
        finally
        {
            CultureInfo.DefaultThreadCurrentUICulture = previousDefault;
            CultureInfo.CurrentUICulture = previousCurrent;
        }
    }

    private static string? ReadFrom(string content)
    {
        using TemporaryDirectory temporary = new();
        string settingsPath = Path.Combine(temporary.Path, "settings.json");
        File.WriteAllText(settingsPath, content);
        return UiCultureBootstrap.Read(settingsPath);
    }
}
