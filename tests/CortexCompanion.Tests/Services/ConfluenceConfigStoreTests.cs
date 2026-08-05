// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text;
using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class ConfluenceConfigStoreTests
{
    private static readonly string[] SinglePage = ["123"];

    [TestMethod]
    public async Task WriteCreatesExactBackupAndValidReplacement()
    {
        using TemporaryDirectory temporary = new();
        string path = Path.Combine(temporary.Path, "confluence.toml");
        byte[] original = V1Bytes();
        await File.WriteAllBytesAsync(path, original);
        ConfluenceConfigStore store = new(path);
        ConfluenceConfigSnapshot snapshot = await store.ReadAsync(CancellationToken.None);
        ConfluenceConfiguration migrated = snapshot.Configuration.MigrateToVersionTwo();
        ConfluenceSpaceConfiguration space = migrated.Spaces[0] with
        {
            Selection = ConfluenceSelection.Pages,
            PageIds = ["123"],
        };

        ConfluenceConfigSnapshot result = await store.WriteAsync(
            migrated.ReplaceSpace(space),
            snapshot.ContentHash,
            CancellationToken.None);

        CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(path + ".bak"));
        Assert.AreEqual(2, result.Configuration.SchemaVersion);
        CollectionAssert.AreEqual(SinglePage, result.Configuration.Spaces[0].PageIds.ToArray());
        Assert.IsFalse(File.Exists(path + ".mutation.lock"));
    }

    [TestMethod]
    public async Task WriteWhenBytesChangedRefusesAndPreservesCurrentFile()
    {
        using TemporaryDirectory temporary = new();
        string path = Path.Combine(temporary.Path, "confluence.toml");
        await File.WriteAllBytesAsync(path, V1Bytes());
        ConfluenceConfigStore store = new(path);
        ConfluenceConfigSnapshot snapshot = await store.ReadAsync(CancellationToken.None);
        byte[] changed = V1Bytes().Concat(new byte[] { 0x0A }).ToArray();
        await File.WriteAllBytesAsync(path, changed);

        await Assert.ThrowsAsync<ConfluenceConfigConflictException>(() => store.WriteAsync(
            snapshot.Configuration.MigrateToVersionTwo(),
            snapshot.ContentHash,
            CancellationToken.None));

        CollectionAssert.AreEqual(changed, await File.ReadAllBytesAsync(path));
        Assert.IsFalse(File.Exists(path + ".bak"));
    }

    [TestMethod]
    public async Task WriteWhenRenderedModelIsInvalidDoesNotReplaceOrCreateBackup()
    {
        using TemporaryDirectory temporary = new();
        string path = Path.Combine(temporary.Path, "confluence.toml");
        byte[] original = V1Bytes();
        await File.WriteAllBytesAsync(path, original);
        ConfluenceConfigStore store = new(path);
        ConfluenceConfigSnapshot snapshot = await store.ReadAsync(CancellationToken.None);
        ConfluenceConfiguration invalid = snapshot.Configuration with { CredentialTarget = string.Empty };

        await Assert.ThrowsAsync<ConfluenceConfigValidationException>(() => store.WriteAsync(
            invalid,
            snapshot.ContentHash,
            CancellationToken.None));

        CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(path));
        Assert.IsFalse(File.Exists(path + ".bak"));
    }

    [TestMethod]
    public async Task MigrationPreservesRawValuesInsteadOfEnvironmentOverrides()
    {
        using TemporaryDirectory temporary = new();
        string path = Path.Combine(temporary.Path, "confluence.toml");
        await File.WriteAllBytesAsync(path, V1Bytes());
        ConfluenceConfigStore store = new(path);
        ConfluenceConfigSnapshot snapshot = await store.ReadAsync(CancellationToken.None);
        IReadOnlyList<ConfluenceEnvironmentOverride> activeOverrides =
            ConfluenceEnvironmentInspector.GetActiveOverrides(name => name switch
            {
                "CORTEX_CONFLUENCE_BASE_URL" => "https://environment.example.test",
                "CORTEX_CONFLUENCE_CREDENTIAL_TARGET" => "environment-target",
                _ => null,
            });

        ConfluenceConfigSnapshot result = await store.WriteAsync(
            snapshot.Configuration.MigrateToVersionTwo(),
            snapshot.ContentHash,
            CancellationToken.None);

        Assert.AreEqual("raw-target", result.Configuration.CredentialTarget);
        Assert.AreEqual("https://raw.example.test", result.Configuration.BaseUrl);
        Assert.AreEqual("docs/source", result.Configuration.Spaces[0].Target);
        Assert.AreEqual(ConfluenceSelection.WholeSpace, result.Configuration.Spaces[0].Selection);
        Assert.HasCount(2, activeOverrides);
    }

    private static byte[] V1Bytes() => Encoding.UTF8.GetBytes("""
        schema_version = 1
        base_url = "https://raw.example.test"
        credential_target = "raw-target"
        max_attachment_size_mb = 50
        failure_threshold = 0.1

        [[spaces]]
        space_key = "DOC"
        target = "docs/source"
        classification = "pro-confidentiel"
        """ + "\n");
}
