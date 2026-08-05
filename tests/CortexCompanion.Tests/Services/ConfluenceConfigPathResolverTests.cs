// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;
using CortexCompanion.Services;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class ConfluenceConfigPathResolverTests
{
    [TestMethod]
    public void RelativeEnvironmentOverrideUsesCliDirectory()
    {
        string cli = Path.GetFullPath(@"C:\Cortex\bin\cortex.exe");
        Dictionary<string, string?> environment = new(StringComparer.OrdinalIgnoreCase)
        {
            ["CORTEX_CONFLUENCE_CONFIG_PATH"] = @"config\confluence.toml",
            ["APPDATA"] = @"C:\Users\Test\AppData\Roaming",
        };

        ConfluenceConfigPathResolution result = ConfluenceConfigPathResolver.Resolve(cli, environment);

        Assert.AreEqual(Path.GetFullPath(@"C:\Cortex\bin\config\confluence.toml"), result.AbsolutePath);
        Assert.AreEqual(ConfluenceConfigPathOrigin.Environment, result.Origin);
        Assert.AreEqual("CORTEX_CONFLUENCE_CONFIG_PATH", result.OriginName);
    }

    [TestMethod]
    public void DefaultUsesAppDataCortexNeighbour()
    {
        Dictionary<string, string?> environment = new(StringComparer.OrdinalIgnoreCase)
        {
            ["APPDATA"] = @"C:\Users\Test\AppData\Roaming",
        };

        ConfluenceConfigPathResolution result = ConfluenceConfigPathResolver.Resolve(
            @"C:\Cortex\cortex.exe",
            environment);

        Assert.AreEqual(
            Path.GetFullPath(@"C:\Users\Test\AppData\Roaming\Cortex\confluence.toml"),
            result.AbsolutePath);
        Assert.AreEqual(ConfluenceConfigPathOrigin.Default, result.Origin);
    }

    [TestMethod]
    public void InspectorOnlyReturnsTheSixSupportedRootFields()
    {
        Dictionary<string, string?> environment = new()
        {
            ["CORTEX_CONFLUENCE_BASE_URL"] = "https://example.test",
            ["CORTEX_CONFLUENCE_SELECTION"] = "pages",
        };

        IReadOnlyList<ConfluenceEnvironmentOverride> result =
            ConfluenceEnvironmentInspector.GetActiveOverrides(name =>
                environment.TryGetValue(name, out string? value) ? value : null);

        Assert.HasCount(1, result);
        Assert.AreEqual("base_url", result[0].FieldName);
    }
}
