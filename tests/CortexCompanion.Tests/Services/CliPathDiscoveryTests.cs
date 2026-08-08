// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class CliPathDiscoveryTests
{
    [TestMethod]
    public void DiscoverPrefersSiblingOverInstalledPath()
    {
        using TemporaryDirectory temporary = new();
        string companionDirectory = Path.Combine(temporary.Path, "companion");
        string installedDirectory = Path.Combine(temporary.Path, "Programs", "Cortex");
        Directory.CreateDirectory(companionDirectory);
        Directory.CreateDirectory(installedDirectory);
        string companionPath = Path.Combine(companionDirectory, "CortexCompanion.exe");
        string siblingPath = Path.Combine(companionDirectory, "cortex.exe");
        string installedPath = Path.Combine(installedDirectory, "cortex.exe");
        File.WriteAllBytes(companionPath, [1]);
        File.WriteAllBytes(siblingPath, [1]);
        File.WriteAllBytes(installedPath, [1]);
        CliPathDiscovery discovery = new(companionPath, temporary.Path);

        string? result = discovery.Discover();

        Assert.AreEqual(Path.GetFullPath(siblingPath), result);
    }

    [TestMethod]
    public void DiscoverUsesInstalledPathWhenSiblingIsAbsent()
    {
        using TemporaryDirectory temporary = new();
        string companionDirectory = Path.Combine(temporary.Path, "companion");
        string installedDirectory = Path.Combine(temporary.Path, "Programs", "Cortex");
        Directory.CreateDirectory(companionDirectory);
        Directory.CreateDirectory(installedDirectory);
        string companionPath = Path.Combine(companionDirectory, "CortexCompanion.exe");
        string installedPath = Path.Combine(installedDirectory, "cortex.exe");
        File.WriteAllBytes(companionPath, [1]);
        File.WriteAllBytes(installedPath, [1]);
        CliPathDiscovery discovery = new(companionPath, temporary.Path);

        string? result = discovery.Discover();

        Assert.AreEqual(Path.GetFullPath(installedPath), result);
    }

    [TestMethod]
    public void DiscoverFindsCortexInCustomCombinedInstallerParent()
    {
        using TemporaryDirectory temporary = new();
        string installationDirectory = Path.Combine(temporary.Path, "Custom Cortex");
        string companionDirectory = Path.Combine(installationDirectory, "Companion");
        Directory.CreateDirectory(companionDirectory);
        string companionPath = Path.Combine(companionDirectory, "CortexCompanion.exe");
        string parentSiblingPath = Path.Combine(installationDirectory, "cortex.exe");
        File.WriteAllBytes(companionPath, [1]);
        File.WriteAllBytes(parentSiblingPath, [1]);
        CliPathDiscovery discovery = new(companionPath, temporary.Path);

        string? result = discovery.Discover();

        Assert.AreEqual(Path.GetFullPath(parentSiblingPath), result);
    }

    [TestMethod]
    public void DiscoverReturnsNullWhenNoInstallerOwnedCandidateExists()
    {
        using TemporaryDirectory temporary = new();
        CliPathDiscovery discovery = new(
            Path.Combine(temporary.Path, "companion", "CortexCompanion.exe"),
            temporary.Path);

        Assert.IsNull(discovery.Discover());
    }
}
