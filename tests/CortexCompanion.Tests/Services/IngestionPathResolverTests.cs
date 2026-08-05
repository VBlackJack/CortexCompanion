// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class IngestionPathResolverTests
{
    [TestMethod]
    public void EnvironmentDataRootOverridesTomlFromEnvironmentSelectedConfig()
    {
        using TemporaryDirectory temporary = new();
        string cliPath = temporary.CreateFakeCli();
        string configPath = Path.Combine(temporary.Path, "selected", "ingestion.toml");
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        File.WriteAllText(configPath, "schema_version = 1\ndata_root = \"toml-root\"\n");
        string environmentRoot = Path.Combine(temporary.Path, "environment-root");
        Dictionary<string, string?> environment = EnvironmentFor(temporary);
        environment["CORTEX_INGESTION_CONFIG_PATH"] = configPath;
        environment["CORTEX_INGESTION_DATA_ROOT"] = environmentRoot;

        IngestionPathResolution result = IngestionPathResolver.Resolve(cliPath, environment);

        Assert.AreEqual(Path.GetFullPath(configPath), result.ConfigPath);
        Assert.AreEqual(Path.GetFullPath(environmentRoot), result.DataRoot);
        Assert.AreEqual(IngestionPathOrigin.Environment, result.ConfigPathOrigin);
        Assert.AreEqual(IngestionPathOrigin.Environment, result.DataRootOrigin);
        Assert.AreEqual(
            Path.Combine(Path.GetFullPath(environmentRoot), "doc", "source-health.json"),
            result.HealthPath);
    }

    [TestMethod]
    public void TomlDataRootOverridesPlatformDefaultAndUsesCliDirectoryForRelativePath()
    {
        using TemporaryDirectory temporary = new();
        string cliPath = temporary.CreateFakeCli();
        string configPath = Path.Combine(temporary.Path, "ingestion.toml");
        File.WriteAllText(configPath, "schema_version = 1\ndata_root = \"relative-data\"\n");
        Dictionary<string, string?> environment = EnvironmentFor(temporary);
        environment["CORTEX_INGESTION_CONFIG_PATH"] = configPath;

        IngestionPathResolution result = IngestionPathResolver.Resolve(cliPath, environment);

        Assert.AreEqual(Path.Combine(temporary.Path, "relative-data"), result.DataRoot);
        Assert.AreEqual(IngestionPathOrigin.Toml, result.DataRootOrigin);
    }

    [TestMethod]
    public void DefaultsUseLocalAppDataWhenBothOverridesAreAbsent()
    {
        using TemporaryDirectory temporary = new();
        Dictionary<string, string?> environment = EnvironmentFor(temporary);

        IngestionPathResolution result = IngestionPathResolver.Resolve(null, environment);

        Assert.AreEqual(
            Path.Combine(temporary.Path, "local", "Cortex", "ingestion"),
            result.DataRoot);
        Assert.AreEqual(IngestionPathOrigin.Default, result.DataRootOrigin);
    }

    [TestMethod]
    public void UnsupportedIngestionTomlSchemaFailsClosed()
    {
        using TemporaryDirectory temporary = new();
        string cliPath = temporary.CreateFakeCli();
        string configPath = Path.Combine(temporary.Path, "ingestion.toml");
        File.WriteAllText(configPath, "schema_version = 2\n");
        Dictionary<string, string?> environment = EnvironmentFor(temporary);
        environment["CORTEX_INGESTION_CONFIG_PATH"] = configPath;

        Assert.ThrowsExactly<IngestionPathResolutionException>(() =>
            IngestionPathResolver.Resolve(cliPath, environment));
    }

    [TestMethod]
    public void RelativeOverrideWithoutCliFailsClosed()
    {
        using TemporaryDirectory temporary = new();
        Dictionary<string, string?> environment = EnvironmentFor(temporary);
        environment["CORTEX_INGESTION_DATA_ROOT"] = "relative";

        Assert.ThrowsExactly<IngestionPathResolutionException>(() =>
            IngestionPathResolver.Resolve(null, environment));
    }

    private static Dictionary<string, string?> EnvironmentFor(TemporaryDirectory temporary) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["APPDATA"] = Path.Combine(temporary.Path, "roaming"),
            ["LOCALAPPDATA"] = Path.Combine(temporary.Path, "local"),
            ["USERPROFILE"] = temporary.Path,
        };
}
