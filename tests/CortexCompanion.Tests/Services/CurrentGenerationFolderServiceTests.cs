// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class CurrentGenerationFolderServiceTests
{
    [TestMethod]
    public async Task ResolveReturnsOnlyTheCurrentContainedDocumentsDirectory()
    {
        using TemporaryDirectory temporary = new();
        string documents = Path.Combine(
            temporary.Path,
            "doc",
            "generations",
            "abc123",
            "documents");
        Directory.CreateDirectory(documents);
        File.WriteAllText(
            Path.Combine(temporary.Path, "doc", "current.json"),
            "{\"schema_version\":1,\"generation_id\":\"abc123\"}");
        IngestionPathResolution resolution = new(
            Path.Combine(temporary.Path, "ingestion.toml"),
            IngestionPathOrigin.Default,
            "APPDATA",
            temporary.Path,
            IngestionPathOrigin.Default,
            "LOCALAPPDATA",
            Path.Combine(temporary.Path, "doc", "source-health.json"),
            2);

        string actual = await CurrentGenerationFolderService.ResolveAsync(
            resolution,
            CancellationToken.None);

        Assert.AreEqual(Path.GetFullPath(documents), actual);
    }

    [TestMethod]
    public async Task ResolveRejectsTraversalGenerationIdentifier()
    {
        using TemporaryDirectory temporary = new();
        Directory.CreateDirectory(Path.Combine(temporary.Path, "doc"));
        File.WriteAllText(
            Path.Combine(temporary.Path, "doc", "current.json"),
            "{\"schema_version\":1,\"generation_id\":\"..\"}");
        IngestionPathResolution resolution = new(
            "ingestion.toml",
            IngestionPathOrigin.Default,
            "APPDATA",
            temporary.Path,
            IngestionPathOrigin.Default,
            "LOCALAPPDATA",
            "source-health.json",
            2);

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            CurrentGenerationFolderService.ResolveAsync(resolution, CancellationToken.None));
    }
}
