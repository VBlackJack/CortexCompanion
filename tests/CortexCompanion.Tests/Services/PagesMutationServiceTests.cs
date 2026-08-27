// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Models;
using CortexCompanion.Services;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class PagesMutationServiceTests
{
    private static readonly string[] SinglePage = ["123"];

    [TestMethod]
    public async Task AddRequiresSuccessfulResolveAndDoesNotPersistTitle()
    {
        FakeCliClient cli = new()
        {
            ResolveResult = Success(new ResolvedPageContract
            {
                ContractVersion = 1,
                PageId = "123",
                Title = "Titre distant",
                SpaceKey = "DOC",
                Configured = false,
            }),
        };
        FakeConfigStore store = new(PagesSnapshot());
        FakeConfirmations confirmations = new() { AddAccepted = true };
        PagesMutationService service = new(cli, store, confirmations);

        bool changed = await service.AddPageAsync("https://wiki/pages/123", false, CancellationToken.None);

        Assert.IsTrue(changed);
        Assert.AreEqual(1, cli.ResolveCalls);
        Assert.AreEqual("Titre distant", confirmations.LastResolvedPage!.Title);
        CollectionAssert.AreEqual(SinglePage, store.WrittenConfiguration!.Spaces[0].PageIds.ToArray());
        Assert.IsFalse(System.Text.Json.JsonSerializer.Serialize(store.WrittenConfiguration)
            .Contains("Titre distant", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task FailedResolveNeverReadsOrWritesConfiguration()
    {
        FakeCliClient cli = new()
        {
            ResolveResult = new ConfluenceCliResult<ResolvedPageContract>(
                CortexExitCode.NotFound,
                null,
                "absente",
                false,
                null),
        };
        FakeConfigStore store = new(PagesSnapshot());
        PagesMutationService service = new(cli, store, new FakeConfirmations());

        await Assert.ThrowsAsync<ConfluenceCliOperationException>(() =>
            service.AddPageAsync("404", false, CancellationToken.None));

        Assert.AreEqual(0, store.ReadCalls);
        Assert.AreEqual(0, store.WriteCalls);
    }

    [TestMethod]
    public async Task WholeSpaceRejectsAddEvenAfterResolve()
    {
        FakeCliClient cli = new()
        {
            ResolveResult = Success(new ResolvedPageContract
            {
                ContractVersion = 1,
                PageId = "123",
                Title = "Titre",
                SpaceKey = "DOC",
                Configured = true,
            }),
        };
        FakeConfigStore store = new(WholeSpaceSnapshot(schemaVersion: 1));
        PagesMutationService service = new(cli, store, new FakeConfirmations());

        await Assert.ThrowsAsync<PageMutationRejectedException>(() =>
            service.AddPageAsync("123", false, CancellationToken.None));

        Assert.AreEqual(0, store.WriteCalls);
    }

    [TestMethod]
    public async Task SpaceOutsideConfigurationIsRefusedWithLocalizedActionableMessage()
    {
        FakeCliClient cli = new()
        {
            ResolveResult = Success(new ResolvedPageContract
            {
                ContractVersion = 1,
                PageId = "123",
                Title = "Titre",
                SpaceKey = "OTHER",
                Configured = false,
            }),
        };
        FakeConfigStore store = new(PagesSnapshot());
        PagesMutationService service = new(cli, store, new FakeConfirmations());

        PageMutationRejectedException exception =
            await Assert.ThrowsAsync<PageMutationRejectedException>(() =>
                service.AddPageAsync("123", false, CancellationToken.None));

        Assert.AreEqual(UiStrings.PagesRejectSpaceNotAllowlisted, exception.Message);
        Assert.AreEqual(0, store.WriteCalls);
    }

    [TestMethod]
    public async Task WrongTypedConfirmationLeavesVersionOneUntouched()
    {
        FakeConfigStore store = new(WholeSpaceSnapshot(schemaVersion: 1));
        FakeConfirmations confirmations = new() { TypedValue = "WRONG" };
        PagesMutationService service = new(new FakeCliClient(), store, confirmations);

        bool changed = await service.SwitchModeAsync("DOC", false, CancellationToken.None);

        Assert.IsFalse(changed);
        Assert.AreEqual(0, store.WriteCalls);
    }

    [TestMethod]
    public async Task VersionOneSwitchMigratesAndCreatesEmptyPagesSelection()
    {
        FakeConfigStore store = new(WholeSpaceSnapshot(schemaVersion: 1));
        FakeConfirmations confirmations = new() { TypedValue = "DOC" };
        PagesMutationService service = new(new FakeCliClient(), store, confirmations);

        bool changed = await service.SwitchModeAsync("DOC", false, CancellationToken.None);

        Assert.IsTrue(changed);
        Assert.AreEqual(2, store.WrittenConfiguration!.SchemaVersion);
        Assert.AreEqual(ConfluenceSelection.Pages, store.WrittenConfiguration.Spaces[0].Selection);
        Assert.IsEmpty(store.WrittenConfiguration.Spaces[0].PageIds);
    }

    [TestMethod]
    public async Task ReadOnlyRejectsBeforeAnyMutationBoundary()
    {
        FakeCliClient cli = new();
        FakeConfigStore store = new(PagesSnapshot());
        PagesMutationService service = new(cli, store, new FakeConfirmations());

        await Assert.ThrowsAsync<PageMutationRejectedException>(() =>
            service.AddPageAsync("123", true, CancellationToken.None));

        Assert.AreEqual(0, cli.ResolveCalls);
        Assert.AreEqual(0, store.ReadCalls);
        Assert.AreEqual(0, store.WriteCalls);
    }

    [TestMethod]
    public async Task CasConflictReloadsCurrentSnapshotForRepresentation()
    {
        FakeCliClient cli = new()
        {
            ResolveResult = Success(new ResolvedPageContract
            {
                ContractVersion = 1,
                PageId = "123",
                Title = "Titre",
                SpaceKey = "DOC",
                Configured = false,
            }),
        };
        ConfluenceConfigSnapshot current = PagesSnapshot() with { ContentHash = new string('b', 64) };
        FakeConfigStore store = new(PagesSnapshot())
        {
            ConflictOnWrite = true,
            ReloadedSnapshot = current,
        };
        PagesMutationService service = new(cli, store, new FakeConfirmations { AddAccepted = true });

        ConfluenceConfigRefreshRequiredException exception =
            await Assert.ThrowsAsync<ConfluenceConfigRefreshRequiredException>(() =>
                service.AddPageAsync("123", false, CancellationToken.None));

        Assert.AreEqual(current.ContentHash, exception.CurrentSnapshot.ContentHash);
        Assert.AreEqual(2, store.ReadCalls);
        Assert.AreEqual(1, store.WriteCalls);
    }

    private static ConfluenceCliResult<ResolvedPageContract> Success(ResolvedPageContract value) =>
        new(CortexExitCode.Ok, value, string.Empty, false, null);

    private static ConfluenceConfigSnapshot PagesSnapshot() => Snapshot(
        new ConfluenceConfiguration(
            2,
            "https://raw.example.test",
            "raw-target",
            null,
            null,
            50,
            0.1,
            [new ConfluenceSpaceConfiguration(
                "DOC", "docs", "pro-confidentiel", ConfluenceSelection.Pages, [])]));

    private static ConfluenceConfigSnapshot WholeSpaceSnapshot(int schemaVersion) => Snapshot(
        new ConfluenceConfiguration(
            schemaVersion,
            "https://raw.example.test",
            "raw-target",
            null,
            null,
            50,
            0.1,
            [new ConfluenceSpaceConfiguration(
                "DOC", "docs", "pro-confidentiel", ConfluenceSelection.WholeSpace, [])]));

    private static ConfluenceConfigSnapshot Snapshot(ConfluenceConfiguration configuration) =>
        new([], new string('a', 64), configuration);

    private sealed class FakeCliClient : IConfluenceCliClient
    {
        public ConfluenceCliResult<ResolvedPageContract> ResolveResult { get; init; } =
            new(CortexExitCode.Error, null, "non configure", false, null);

        public int ResolveCalls { get; private set; }

        public Task<ConfluenceCliResult<PagesContract>> GetPagesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ConfluenceCliResult<ResolvedPageContract>> ResolveAsync(
            string reference,
            CancellationToken cancellationToken)
        {
            ResolveCalls++;
            return Task.FromResult(ResolveResult);
        }
    }

    private sealed class FakeConfigStore(ConfluenceConfigSnapshot snapshot) : IConfluenceConfigStore
    {
        public bool ConflictOnWrite { get; init; }

        public ConfluenceConfigSnapshot? ReloadedSnapshot { get; init; }

        public int ReadCalls { get; private set; }

        public int WriteCalls { get; private set; }

        public ConfluenceConfiguration? WrittenConfiguration { get; private set; }

        public Task<ConfluenceConfigSnapshot> ReadAsync(CancellationToken cancellationToken)
        {
            ReadCalls++;
            return Task.FromResult(ReadCalls > 1 && ReloadedSnapshot is not null ? ReloadedSnapshot : snapshot);
        }

        public Task<ConfluenceConfigSnapshot> WriteAsync(
            ConfluenceConfiguration configuration,
            string? expectedHash,
            CancellationToken cancellationToken)
        {
            WriteCalls++;
            if (ConflictOnWrite)
            {
                throw new ConfluenceConfigConflictException(
                    "Confluence configuration changed after the caller snapshot.");
            }

            WrittenConfiguration = configuration;
            return Task.FromResult(new ConfluenceConfigSnapshot([], expectedHash ?? new string('0', 64), configuration));
        }
    }

    private sealed class FakeConfirmations : IPageMutationConfirmationService
    {
        public bool AddAccepted { get; init; }

        public string? TypedValue { get; init; }

        public ResolvedPageContract? LastResolvedPage { get; private set; }

        public bool ConfirmAdd(ResolvedPageContract page)
        {
            LastResolvedPage = page;
            return AddAccepted;
        }

        public bool ConfirmRemove(string spaceKey, string pageId, string? title) => true;

        public string? ConfirmModeChange(
            string spaceKey,
            ConfluenceSelection targetSelection,
            IReadOnlyList<string> targetPageIds) => TypedValue;
    }
}
