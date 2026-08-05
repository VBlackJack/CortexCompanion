// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class PatBadgeServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 5, 10, 0, 0, TimeSpan.Zero);

    [TestMethod]
    [DataRow(29, PatBadgeState.Warning)]
    [DataRow(30, PatBadgeState.Ok)]
    [DataRow(31, PatBadgeState.Ok)]
    [DataRow(-1, PatBadgeState.Expired)]
    public async Task NamedThirtyDayBoundaryIsExact(int days, PatBadgeState expected)
    {
        string value = Now.AddDays(days).ToString("O");

        PatBadgeResult result = await PatBadgeService.ReadAsync(
            null,
            Now,
            name => name == "CORTEX_CONFLUENCE_AUTH_EXPIRES_AT" ? value : null);

        Assert.AreEqual(expected, result.State);
        Assert.AreEqual("CORTEX_CONFLUENCE_AUTH_EXPIRES_AT", result.Origin);
    }

    [TestMethod]
    public async Task EnvironmentOverridesTomlAndRetainsOrigin()
    {
        using TemporaryDirectory temporary = new();
        string configPath = Path.Combine(temporary.Path, "confluence.toml");
        File.WriteAllText(configPath, "schema_version = 1\nauth_expires_at = 2026-11-01T00:00:00+01:00\n");
        string environmentExpiry = Now.AddDays(29).ToString("O");

        PatBadgeResult result = await PatBadgeService.ReadAsync(
            configPath,
            Now,
            name => name == "CORTEX_CONFLUENCE_AUTH_EXPIRES_AT" ? environmentExpiry : null);

        Assert.AreEqual(PatBadgeState.Warning, result.State);
        Assert.AreEqual("CORTEX_CONFLUENCE_AUTH_EXPIRES_AT", result.Origin);
    }

    [TestMethod]
    public async Task TomlOnlyUsesRawConfiguredExpiry()
    {
        using TemporaryDirectory temporary = new();
        string configPath = Path.Combine(temporary.Path, "confluence.toml");
        File.WriteAllText(configPath, "schema_version = 1\nauth_expires_at = 2026-11-01T00:00:00+01:00\n");

        PatBadgeResult result = await PatBadgeService.ReadAsync(configPath, Now, _ => null);

        Assert.AreEqual(PatBadgeState.Ok, result.State);
        Assert.AreEqual(configPath, result.Origin);
    }

    [TestMethod]
    public async Task MissingValueIsExplicitlyUnknown()
    {
        PatBadgeResult result = await PatBadgeService.ReadAsync(null, Now, _ => null);

        Assert.AreEqual(PatBadgeState.Unknown, result.State);
        Assert.IsNull(result.ExpiresAt);
    }
}
