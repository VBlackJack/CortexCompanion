// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;
using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class SettingsStoreTests
{
    [TestMethod]
    public async Task SaveAndLoadAsyncNewFileRoundTripsApplicationSettings()
    {
        using TemporaryDirectory temporaryDirectory = new();
        string settingsPath = Path.Combine(temporaryDirectory.Path, "app", "settings.json");
        SettingsStore store = new(settingsPath);
        AppSettings expected = new(@"C:\Tools\cortex.exe", 60);

        await store.SaveAsync(expected);
        SettingsLoadResult loaded = await store.LoadAsync();
        string storedJson = await File.ReadAllTextAsync(settingsPath);

        Assert.AreEqual(SettingsLoadState.Loaded, loaded.State);
        Assert.AreEqual(expected, loaded.Settings);
        Assert.IsFalse(storedJson.Contains("effectiveCliTimeoutSeconds", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task LoadAsyncLegacyFileUsesDefaultCliTimeout()
    {
        using TemporaryDirectory temporaryDirectory = new();
        string settingsPath = Path.Combine(temporaryDirectory.Path, "settings.json");
        await File.WriteAllTextAsync(
            settingsPath,
            """{"cliPath":"C:\\Tools\\cortex.exe"}""");
        SettingsStore store = new(settingsPath);

        SettingsLoadResult loaded = await store.LoadAsync();

        Assert.AreEqual(SettingsLoadState.Loaded, loaded.State);
        Assert.AreEqual(
            AppConstants.DefaultCliTimeoutSeconds,
            loaded.Settings.EffectiveCliTimeoutSeconds);
    }

    [TestMethod]
    public async Task LoadAsyncUnsupportedCliTimeoutUsesSafeDefault()
    {
        using TemporaryDirectory temporaryDirectory = new();
        string settingsPath = Path.Combine(temporaryDirectory.Path, "settings.json");
        await File.WriteAllTextAsync(
            settingsPath,
            """{"cliPath":null,"cliHandshakeTimeoutSeconds":999}""");
        SettingsStore store = new(settingsPath);

        SettingsLoadResult loaded = await store.LoadAsync();

        Assert.AreEqual(SettingsLoadState.Loaded, loaded.State);
        Assert.AreEqual(
            AppConstants.DefaultCliTimeoutSeconds,
            loaded.Settings.EffectiveCliTimeoutSeconds);
    }

    [TestMethod]
    public async Task SaveAsyncExistingFileReplacesAtomicallyWithoutTemporaryRemnants()
    {
        using TemporaryDirectory temporaryDirectory = new();
        string settingsPath = Path.Combine(temporaryDirectory.Path, "settings.json");
        SettingsStore store = new(settingsPath);
        await store.SaveAsync(new AppSettings(@"C:\Old\cortex.exe"));

        AppSettings expected = new(@"C:\New\cortex.exe");
        await store.SaveAsync(expected);
        SettingsLoadResult loaded = await store.LoadAsync();
        string[] temporaryFiles = Directory.GetFiles(temporaryDirectory.Path, "*.tmp");

        Assert.AreEqual(expected, loaded.Settings);
        Assert.HasCount(0, temporaryFiles);
    }

    [TestMethod]
    public async Task LoadAsyncCorruptFileReturnsUnconfiguredState()
    {
        using TemporaryDirectory temporaryDirectory = new();
        string settingsPath = Path.Combine(temporaryDirectory.Path, "settings.json");
        await File.WriteAllTextAsync(settingsPath, "{not-json");
        SettingsStore store = new(settingsPath);

        SettingsLoadResult loaded = await store.LoadAsync();

        Assert.AreEqual(SettingsLoadState.Corrupt, loaded.State);
        Assert.AreEqual(AppSettings.Empty, loaded.Settings);
        Assert.IsNull(loaded.Settings.CliPath);
    }

    [TestMethod]
    public async Task LoadAsyncMissingFileReturnsUnconfiguredState()
    {
        using TemporaryDirectory temporaryDirectory = new();
        SettingsStore store = new(Path.Combine(temporaryDirectory.Path, "settings.json"));

        SettingsLoadResult loaded = await store.LoadAsync();

        Assert.AreEqual(SettingsLoadState.Missing, loaded.State);
        Assert.AreEqual(AppSettings.Empty, loaded.Settings);
    }
}
