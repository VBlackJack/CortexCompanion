// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
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
    [DataRow("page", "100", true, DisplayName = "the source lists the page")]
    [DataRow("subtree", "7", true, DisplayName = "a tracked subtree covers the page")]
    [DataRow("none", null, false, DisplayName = "the source does not collect the page")]
    public async Task TheScopeWindowIsToldWhatThePreviewSaysAboutCoverage(
        string coverage,
        string? root,
        bool expected)
    {
        // The window cannot settle this itself: what happens to a page the source already
        // collects depends on the scope chosen there. The answer comes from Cortex, which is
        // the only side that can see a page covered through a tracked subtree.
        using TemporaryDirectory directory = new();
        string config = Path.Combine(directory.Path, "confluence.toml");
        ConfluenceConfigStore store = new(config);
        await store.WriteAsync(Configuration("DOC", ConfluenceSelection.Subtree, ["7"]), null, CancellationToken.None);
        Confirmations confirmations = new(null);
        ConfluenceSourceService service = new(
            store, new(store, TimeProvider.System), _ => new Client(coverage: coverage, root: root), confirmations);

        await service.AddAsync(Request(null), false, CancellationToken.None);

        Assert.AreEqual(expected, confirmations.WasToldAlreadyCovered);
    }

    [TestMethod]
    [DataRow(ConfluenceSelection.Pages, new[] { "100", "7" }, "page", ConfluenceSelection.Subtree, SourceMergeChoice.Widen,
        ConfluenceSelection.Subtree, new[] { "100", "7" }, DisplayName = "widening pages to a subtree makes every page a root")]
    [DataRow(ConfluenceSelection.Pages, new[] { "100", "7" }, "page", ConfluenceSelection.Subtree, SourceMergeChoice.Replace,
        ConfluenceSelection.Subtree, new[] { "100" }, DisplayName = "replacing keeps the chosen page alone")]
    [DataRow(ConfluenceSelection.Subtree, new[] { "7" }, "subtree", ConfluenceSelection.Pages, SourceMergeChoice.Widen,
        ConfluenceSelection.Subtree, new[] { "7", "100" }, DisplayName = "widening a subtree by a page adds a root")]
    [DataRow(ConfluenceSelection.Subtree, new[] { "7" }, "subtree", ConfluenceSelection.Pages, SourceMergeChoice.Replace,
        ConfluenceSelection.Pages, new[] { "100" }, DisplayName = "replacing a subtree by a page narrows the source")]
    [DataRow(ConfluenceSelection.Pages, new[] { "100" }, "page", ConfluenceSelection.WholeSpace, SourceMergeChoice.Widen,
        ConfluenceSelection.WholeSpace, new string[0], DisplayName = "widening to the whole space drops the list")]
    [DataRow(ConfluenceSelection.WholeSpace, new string[0], "whole_space", ConfluenceSelection.Subtree, SourceMergeChoice.Replace,
        ConfluenceSelection.Subtree, new[] { "100" }, DisplayName = "replacing a whole space by a subtree narrows the source")]
    public async Task ACoveredPageIsMergedTheWayTheUserChose(
        ConfluenceSelection existing,
        string[] existingIds,
        string coverage,
        ConfluenceSelection chosen,
        SourceMergeChoice choice,
        ConfluenceSelection expectedSelection,
        string[] expectedIds)
    {
        using TemporaryDirectory directory = new();
        ConfluenceConfigStore store = new(Path.Combine(directory.Path, "confluence.toml"));
        await store.WriteAsync(Configuration("DOC", existing, existingIds), null, CancellationToken.None);
        string? root = coverage == "page" ? "100" : coverage == "subtree" ? "7" : null;
        Confirmations confirmations = new(chosen) { MergeChoice = choice };
        ConfluenceSourceService service = new(
            store, new(store, TimeProvider.System), _ => new Client(coverage: coverage, root: root), confirmations);

        bool result = await service.AddAsync(Request(null), false, CancellationToken.None);

        Assert.IsTrue(result);
        Assert.IsTrue(service.LastSaveMerged);
        ConfluenceSpaceConfiguration saved = (await store.ReadAsync(CancellationToken.None)).Configuration.Spaces
            .Single(space => space.SpaceKey == "DOC");
        Assert.AreEqual(expectedSelection, saved.Selection);
        CollectionAssert.AreEqual(expectedIds, saved.PageIds.ToArray());
        Assert.IsNotNull(confirmations.LastMerge);
        Assert.AreEqual("DOC", confirmations.LastMerge.Existing.SpaceKey);
        // The fake client has no page tree, so the review says it could not measure the documents.
        SourceChangeReview offered = (choice == SourceMergeChoice.Widen ? confirmations.LastMerge.Widen : confirmations.LastMerge.Replace)!;
        Assert.IsFalse(offered.IsComplete);
        Assert.AreEqual(expectedSelection, offered.After.Selection);
    }

    [TestMethod]
    public async Task AMergeThatChangesNothingEitherWayIsRefusedBeforeAnyReview()
    {
        using TemporaryDirectory directory = new();
        string config = Path.Combine(directory.Path, "confluence.toml");
        ConfluenceConfigStore store = new(config);
        await store.WriteAsync(Configuration("DOC", ConfluenceSelection.Pages, ["100"]), null, CancellationToken.None);
        byte[] before = await File.ReadAllBytesAsync(config);
        Confirmations confirmations = new(ConfluenceSelection.Pages) { MergeChoice = SourceMergeChoice.Widen };
        ConfluenceSourceService service = new(
            store, new(store, TimeProvider.System), _ => new Client(coverage: "page", root: "100"), confirmations);

        PageMutationRejectedException rejected = await Assert.ThrowsAsync<PageMutationRejectedException>(
            () => service.AddAsync(Request(null), false, CancellationToken.None));

        Assert.AreEqual(UiStrings.PagesRejectPageAlreadyConfigured, rejected.Message);
        Assert.IsNull(confirmations.LastMerge);
        CollectionAssert.AreEqual(before, await File.ReadAllBytesAsync(config));
    }

    [TestMethod]
    public async Task AWholeSpaceSourceOffersOnlyToReplaceItsSelection()
    {
        using TemporaryDirectory directory = new();
        ConfluenceConfigStore store = new(Path.Combine(directory.Path, "confluence.toml"));
        await store.WriteAsync(Configuration("DOC", ConfluenceSelection.WholeSpace, []), null, CancellationToken.None);
        Confirmations confirmations = new(ConfluenceSelection.Pages);
        ConfluenceSourceService service = new(
            store, new(store, TimeProvider.System), _ => new Client(coverage: "whole_space", root: null), confirmations);

        bool result = await service.AddAsync(Request(null), false, CancellationToken.None);

        Assert.IsFalse(result);
        Assert.IsNotNull(confirmations.LastMerge);
        Assert.IsNull(confirmations.LastMerge.Widen);
        Assert.IsNotNull(confirmations.LastMerge.Replace);
        Assert.AreEqual(ConfluenceSelection.Pages, confirmations.LastMerge.Replace.After.Selection);
    }

    [TestMethod]
    public async Task CancellingTheMergeWritesNothing()
    {
        using TemporaryDirectory directory = new();
        string config = Path.Combine(directory.Path, "confluence.toml");
        ConfluenceConfigStore store = new(config);
        await store.WriteAsync(Configuration("DOC", ConfluenceSelection.Subtree, ["7"]), null, CancellationToken.None);
        byte[] before = await File.ReadAllBytesAsync(config);
        Confirmations confirmations = new(ConfluenceSelection.Subtree);
        ConfluenceSourceService service = new(
            store, new(store, TimeProvider.System), _ => new Client(coverage: "subtree", root: "7"), confirmations);

        bool result = await service.AddAsync(Request(null), false, CancellationToken.None);

        Assert.IsFalse(result);
        Assert.IsFalse(service.LastSaveMerged);
        CollectionAssert.AreEqual(before, await File.ReadAllBytesAsync(config));
    }

    private static ConfluenceConfiguration Configuration(string spaceKey, ConfluenceSelection selection, string[] pageIds) =>
        Configuration() with
        {
            SchemaVersion = selection == ConfluenceSelection.Subtree ? 3 : 2,
            Spaces = [new(spaceKey, $"confluence/{spaceKey}", "pro-confidentiel", selection, pageIds)],
        };

    private static ConfluenceConfiguration Configuration() => new(2, "https://wiki.example.test", "cortex-spike",
        new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero), null, 50, 0.1,
        [new("OLD", "confluence/OLD", "pro-confidentiel", ConfluenceSelection.Pages, ["99"])]);

    private sealed class Client(CortexExitCode code = CortexExitCode.Ok, string coverage = "none", string? root = null) : IConfluenceCliClient
    {
        public Task<ConfluenceCliResult<PagesContract>> GetPagesAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ConfluenceCliResult<ResolvedPageContract>> ResolveAsync(string reference, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ConfluenceCliResult<ScopePreviewContract>> PreviewAsync(string reference, CancellationToken cancellationToken) =>
            Task.FromResult(new ConfluenceCliResult<ScopePreviewContract>(code, code == CortexExitCode.Ok ? new()
            {
                ContractVersion = 2,
                PageId = "100",
                Title = "Root",
                SpaceKey = "DOC",
                RecommendedSelection = "subtree",
                Coverage = coverage,
                CoveringRoot = root,
                PageOnly = new() { PageCount = 1, EstimatedBytes = 100 },
                Subtree = new() { PageCount = 3, EstimatedBytes = 300 },
                WholeSpace = new() { PageCount = 9, EstimatedBytes = 900 },
                StorageRoot = "C:\\fixture",
                RetentionGenerations = 2,
            } : null, string.Empty, false, null));
    }

    private sealed class Confirmations(ConfluenceSelection? selection, Action? beforeConfirm = null) : IPageMutationConfirmationService
    {
        /// <summary>Records what the window was told about the source already collecting the page.</summary>
        public bool? WasToldAlreadyCovered { get; private set; }

        /// <summary>Gets or sets the merge this fake chooses, null standing for a cancelled review.</summary>
        public SourceMergeChoice? MergeChoice { get; init; }

        /// <summary>Records the last merge review this fake was shown.</summary>
        public SourceMergeReview? LastMerge { get; private set; }

        public ConfluenceSelection? ChooseScope(ScopePreviewContract preview) { beforeConfirm?.Invoke(); return selection; }

        public ConfluenceSelection? ChooseScope(ScopePreviewContract preview, bool alreadyCovered)
        {
            WasToldAlreadyCovered = alreadyCovered;
            return ChooseScope(preview);
        }

        public SourceMergeChoice? ChooseMerge(SourceMergeReview review)
        {
            LastMerge = review;
            return MergeChoice;
        }
        public bool ConfirmAdd(ResolvedPageContract page) => throw new NotSupportedException();
        public bool ConfirmAddSpace(string spaceKey, string classification) => throw new NotSupportedException();
        public bool ConfirmKeepEmptySpace(string spaceKey) => throw new NotSupportedException();
        public bool ConfirmRemove(string spaceKey, string pageId, string? title) => throw new NotSupportedException();
        public string? ConfirmModeChange(string spaceKey, ConfluenceSelection targetSelection, IReadOnlyList<string> targetPageIds) => throw new NotSupportedException();
    }
}
