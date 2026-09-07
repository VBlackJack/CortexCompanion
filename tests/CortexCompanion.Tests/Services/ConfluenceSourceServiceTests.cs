// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Interfaces;
using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

/// <summary>Exercises the real isolated preview and atomic configuration boundary.</summary>
[TestClass]
public sealed class ConfluenceSourceServiceTests
{
    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task PreviewDoesNotPersistUntilConfirmation(bool existing, bool accept)
    {
        using TemporaryDirectory directory = new();
        string config = Path.Combine(directory.Path, "confluence.toml");
        string converter = Path.Combine(directory.Path, "converter.exe");
        await File.WriteAllBytesAsync(converter, [0x4d, 0x5a]);
        ConfluenceConfigStore store = new(config);
        if (existing) { await store.WriteAsync(Configuration(), null, CancellationToken.None); }
        byte[]? before = existing ? await File.ReadAllBytesAsync(config) : null;
        string? previewPath = null;
        StubProcessRunner runner = new(ProcessRunResult.Completed(0,
            "{\"tool_version\":\"1.2.0\",\"schema_version\":1}", string.Empty));
        ConfluenceSetupService setup = new(store, new ConfluenceConverterProbe(runner), converter);
        Confirmations confirmations = new(accept ? ConfluenceSelection.Subtree : null);
        ConfluenceSourceService service = new(store, setup, path =>
        {
            previewPath = path;
            Assert.IsTrue(File.Exists(path));
            if (before is null) { Assert.IsFalse(File.Exists(config)); }
            else { CollectionAssert.AreEqual(before, File.ReadAllBytes(config)); }
            string candidate = File.ReadAllText(path);
            StringAssert.Contains(candidate, "DOC");
            Assert.DoesNotContain("token =", candidate);
            return new Client();
        }, confirmations);

        bool result = await service.AddAsync(Request(converter), false, CancellationToken.None);

        Assert.AreEqual(accept, result);
        Assert.IsFalse(File.Exists(previewPath));
        if (accept)
        {
            ConfluenceConfigSnapshot saved = await store.ReadAsync(CancellationToken.None);
            ConfluenceSpaceConfiguration source = saved.Configuration.Spaces.Single(space => space.SpaceKey == "DOC");
            Assert.AreEqual(ConfluenceSelection.Subtree, source.Selection);
            Assert.HasCount(1, source.PageIds);
            Assert.AreEqual("100", source.PageIds[0]);
            Assert.AreEqual(3, saved.Configuration.SchemaVersion);
            Assert.HasCount(existing ? 2 : 1, saved.Configuration.Spaces);
        }
        else if (before is null) { Assert.IsFalse(File.Exists(config)); }
        else { CollectionAssert.AreEqual(before, await File.ReadAllBytesAsync(config)); }
    }

    [TestMethod]
    [DataRow(CortexExitCode.Auth)]
    [DataRow(CortexExitCode.Remote)]
    public async Task FailedPreviewLeavesConfigurationIdentical(CortexExitCode failure)
    {
        using TemporaryDirectory directory = new();
        string config = Path.Combine(directory.Path, "confluence.toml");
        ConfluenceConfigStore store = new(config);
        await store.WriteAsync(Configuration(), null, CancellationToken.None);
        byte[] before = await File.ReadAllBytesAsync(config);
        string? previewPath = null;
        ConfluenceSourceService service = new(store, new(store, TimeProvider.System), path =>
        {
            previewPath = path;
            return new Client(failure);
        }, new Confirmations(ConfluenceSelection.Subtree));
        await Assert.ThrowsAsync<ConfluenceCliOperationException>(() => service.AddAsync(Request(null), false, CancellationToken.None));
        CollectionAssert.AreEqual(before, await File.ReadAllBytesAsync(config));
        Assert.IsFalse(File.Exists(previewPath));
    }

    [TestMethod]
    public async Task ConcurrentEditAfterPreviewIsPreserved()
    {
        using TemporaryDirectory directory = new();
        string config = Path.Combine(directory.Path, "confluence.toml");
        ConfluenceConfigStore store = new(config);
        await store.WriteAsync(Configuration(), null, CancellationToken.None);
        byte[] concurrent = [];
        Confirmations confirmations = new(ConfluenceSelection.Subtree, () =>
        {
            File.AppendAllText(config, "\n# concurrent editor\n");
            concurrent = File.ReadAllBytes(config);
        });
        ConfluenceSourceService service = new(store, new(store, TimeProvider.System), _ => new Client(), confirmations);
        await Assert.ThrowsAsync<ConfluenceConfigConflictException>(() => service.AddAsync(Request(null), false, CancellationToken.None));
        CollectionAssert.AreEqual(concurrent, await File.ReadAllBytesAsync(config));
    }

    [TestMethod]
    public async Task ForeignOriginIsRejectedBeforePreview()
    {
        using TemporaryDirectory directory = new();
        ConfluenceConfigStore store = new(Path.Combine(directory.Path, "confluence.toml"));
        await store.WriteAsync(Configuration(), null, CancellationToken.None);
        ConfluenceSourceService service = new(store, new(store, TimeProvider.System),
            _ => throw new AssertFailedException("Preview must not contact another origin."), new Confirmations(null));
        await Assert.ThrowsAsync<PageMutationRejectedException>(() => service.AddAsync(
            Request(null) with { PageUrl = "https://foreign.example.test/spaces/DOC" }, false, CancellationToken.None));
    }

    private static ConfluenceSetupRequest Request(string? converter) => new(
        "https://wiki.example.test/spaces/DOC", "DOC", new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero), converter, "pro-confidentiel");

    [TestMethod]
    [DataRow("100", true, DisplayName = "the page is already one of the tracked roots")]
    [DataRow("42", false, DisplayName = "the page is not tracked yet")]
    public async Task TheScopeWindowIsToldWhetherThePageIsAlreadyTracked(
        string trackedPageId,
        bool expected)
    {
        // The window cannot settle this itself: whether an already tracked page can be added
        // depends on the scope chosen there. Telling it is what stops the user from choosing
        // carefully and only then being refused.
        using TemporaryDirectory directory = new();
        string config = Path.Combine(directory.Path, "confluence.toml");
        string converter = Path.Combine(directory.Path, "converter.exe");
        await File.WriteAllBytesAsync(converter, [0x4d, 0x5a]);
        ConfluenceConfigStore store = new(config);
        await store.WriteAsync(
            Configuration() with
            {
                Spaces =
                [
                    new("DOC", "confluence/DOC", "pro-confidentiel",
                        ConfluenceSelection.Pages, [trackedPageId]),
                ],
            },
            null,
            CancellationToken.None);
        StubProcessRunner runner = new(ProcessRunResult.Completed(0,
            "{\"tool_version\":\"1.2.0\",\"schema_version\":1}", string.Empty));
        Confirmations confirmations = new(null);
        ConfluenceSourceService service = new(
            store,
            new ConfluenceSetupService(store, new ConfluenceConverterProbe(runner), converter),
            _ => new Client(),
            confirmations);

        await service.AddAsync(Request(converter), false, CancellationToken.None);

        Assert.AreEqual(expected, confirmations.WasToldAlreadyTracked);
    }

    private static ConfluenceConfiguration Configuration() => new(2, "https://wiki.example.test", "cortex-spike",
        new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero), null, 50, 0.1,
        [new("OLD", "confluence/OLD", "pro-confidentiel", ConfluenceSelection.Pages, ["99"])]);

    private sealed class Client(CortexExitCode code = CortexExitCode.Ok) : IConfluenceCliClient
    {
        public Task<ConfluenceCliResult<PagesContract>> GetPagesAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ConfluenceCliResult<ResolvedPageContract>> ResolveAsync(string reference, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ConfluenceCliResult<ScopePreviewContract>> PreviewAsync(string reference, CancellationToken cancellationToken) =>
            Task.FromResult(new ConfluenceCliResult<ScopePreviewContract>(code, code == CortexExitCode.Ok ? new()
            {
                ContractVersion = 1,
                PageId = "100",
                Title = "Root",
                SpaceKey = "DOC",
                RecommendedSelection = "subtree",
                PageOnly = new() { PageCount = 1, EstimatedBytes = 100 },
                Subtree = new() { PageCount = 3, EstimatedBytes = 300 },
                WholeSpace = new() { PageCount = 9, EstimatedBytes = 900 },
                StorageRoot = "C:\\fixture",
                RetentionGenerations = 2,
            } : null, string.Empty, false, null));
    }

    private sealed class Confirmations(ConfluenceSelection? selection, Action? beforeConfirm = null) : IPageMutationConfirmationService
    {
        /// <summary>Records what the window was told about the page already being tracked.</summary>
        public bool? WasToldAlreadyTracked { get; private set; }

        public ConfluenceSelection? ChooseScope(ScopePreviewContract preview) { beforeConfirm?.Invoke(); return selection; }

        public ConfluenceSelection? ChooseScope(ScopePreviewContract preview, bool alreadyTracked)
        {
            WasToldAlreadyTracked = alreadyTracked;
            return ChooseScope(preview);
        }
        public bool ConfirmAdd(ResolvedPageContract page) => throw new NotSupportedException();
        public bool ConfirmAddSpace(string spaceKey, string classification) => throw new NotSupportedException();
        public bool ConfirmKeepEmptySpace(string spaceKey) => throw new NotSupportedException();
        public bool ConfirmRemove(string spaceKey, string pageId, string? title) => throw new NotSupportedException();
        public string? ConfirmModeChange(string spaceKey, ConfluenceSelection targetSelection, IReadOnlyList<string> targetPageIds) => throw new NotSupportedException();
    }
}
