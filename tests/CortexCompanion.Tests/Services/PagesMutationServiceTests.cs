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
    private static readonly string[] AnssiwsPage = ["1683736048"];

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
    public async Task ExplicitSubtreeChoiceMigratesSchemaAndPersistsRoot()
    {
        FakeCliClient cli = new()
        {
            ResolveResult = Success(new ResolvedPageContract
            {
                ContractVersion = 1,
                PageId = "123",
                Title = "Root",
                SpaceKey = "DOC",
                Configured = false,
            }),
        };
        FakeConfigStore store = new(PagesSnapshot());
        FakeConfirmations confirmations = new()
        {
            AddAccepted = true,
            SelectedScope = ConfluenceSelection.Subtree,
        };
        PagesMutationService service = new(cli, store, confirmations);

        Assert.IsTrue(await service.AddPageAsync("123", false, CancellationToken.None));

        Assert.AreEqual(3, store.WrittenConfiguration!.SchemaVersion);
        Assert.AreEqual(ConfluenceSelection.Subtree, store.WrittenConfiguration.Spaces[0].Selection);
        CollectionAssert.AreEqual(SinglePage, store.WrittenConfiguration.Spaces[0].PageIds.ToArray());
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
    [DataRow(ConfluenceSelection.WholeSpace, ConfluenceSelection.Pages)]
    [DataRow(ConfluenceSelection.Pages, ConfluenceSelection.Subtree)]
    [DataRow(ConfluenceSelection.Subtree, ConfluenceSelection.WholeSpace)]
    public async Task ModeSwitchCyclesWholeSpaceThenPagesThenSubtree(
        ConfluenceSelection current,
        ConfluenceSelection expected)
    {
        IReadOnlyList<string> pageIds = current == ConfluenceSelection.WholeSpace ? [] : SinglePage;
        FakeConfigStore store = new(SelectionSnapshot(current, pageIds, schemaVersion: 3));
        FakeConfirmations confirmations = new() { TypedValue = "DOC" };
        PagesMutationService service = new(new FakeCliClient(), store, confirmations);

        Assert.IsTrue(await service.SwitchModeAsync("DOC", false, CancellationToken.None));

        Assert.AreEqual(expected, confirmations.LastTargetSelection);
        Assert.AreEqual(expected, store.WrittenConfiguration!.Spaces[0].Selection);
    }

    [TestMethod]
    public async Task SwitchingPagesToSubtreeKeepsEveryIdentifierAsARoot()
    {
        FakeConfigStore store = new(SelectionSnapshot(ConfluenceSelection.Pages, SinglePage));
        FakeConfirmations confirmations = new() { TypedValue = "DOC" };
        PagesMutationService service = new(new FakeCliClient(), store, confirmations);

        Assert.IsTrue(await service.SwitchModeAsync("DOC", false, CancellationToken.None));

        CollectionAssert.AreEqual(SinglePage, confirmations.LastTargetPageIds!.ToArray());
        CollectionAssert.AreEqual(SinglePage, store.WrittenConfiguration!.Spaces[0].PageIds.ToArray());
        Assert.AreEqual(3, store.WrittenConfiguration.SchemaVersion);
    }

    [TestMethod]
    public async Task OneClickScopeCorrectionPreservesRootsAndExpandsToSubtree()
    {
        FakeConfigStore store = new(SelectionSnapshot(ConfluenceSelection.Pages, SinglePage));
        PagesMutationService service = new(new FakeCliClient(), store, new FakeConfirmations());

        Assert.IsTrue(await service.ExpandToSubtreeAsync(
            "DOC",
            false,
            CancellationToken.None));

        Assert.AreEqual(3, store.WrittenConfiguration!.SchemaVersion);
        Assert.AreEqual(ConfluenceSelection.Subtree, store.WrittenConfiguration.Spaces[0].Selection);
        CollectionAssert.AreEqual(SinglePage, store.WrittenConfiguration.Spaces[0].PageIds.ToArray());
    }

    [TestMethod]
    public async Task SwitchingSubtreeToWholeSpaceDropsEveryRoot()
    {
        FakeConfigStore store = new(
            SelectionSnapshot(ConfluenceSelection.Subtree, SinglePage, schemaVersion: 3));
        FakeConfirmations confirmations = new() { TypedValue = "DOC" };
        PagesMutationService service = new(new FakeCliClient(), store, confirmations);

        Assert.IsTrue(await service.SwitchModeAsync("DOC", false, CancellationToken.None));

        Assert.AreEqual(ConfluenceSelection.WholeSpace, store.WrittenConfiguration!.Spaces[0].Selection);
        Assert.IsEmpty(store.WrittenConfiguration.Spaces[0].PageIds);
    }

    [TestMethod]
    public async Task SubtreeRootsCanBeRemovedLikeAnyExplicitIdentifier()
    {
        FakeConfigStore store = new(
            SelectionSnapshot(ConfluenceSelection.Subtree, SinglePage, schemaVersion: 3));
        PagesMutationService service = new(
            new FakeCliClient(),
            store,
            new FakeConfirmations { KeepEmptySpaceAccepted = true });

        bool changed = await service.RemovePageAsync(
            "DOC",
            "123",
            null,
            false,
            CancellationToken.None);

        Assert.IsTrue(changed);
        Assert.IsEmpty(store.WrittenConfiguration!.Spaces[0].PageIds);
        Assert.AreEqual(ConfluenceSelection.Subtree, store.WrittenConfiguration.Spaces[0].Selection);
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

    [TestMethod]
    public async Task AddSpaceAllowlistsTheSpaceAndAddsThePageItCameFrom()
    {
        FakeConfigStore store = new(PagesSnapshot()) { ReflectWrites = true };
        FakeConfirmations confirmations = new()
        {
            AddSpaceAccepted = true,
            AddAccepted = true,
            SelectedScope = ConfluenceSelection.Subtree,
        };
        PagesMutationService service = new(AnssiwsCliClient(), store, confirmations);

        bool added = await service.AddSpaceAsync(
            "https://raw.example.test/spaces/ANSSIWS/pages/1683736048/ADSEC+Platform",
            "pro-confidentiel",
            false,
            CancellationToken.None);

        Assert.IsTrue(added);
        Assert.AreEqual("pro-confidentiel", confirmations.LastClassification);
        Assert.AreEqual(0, confirmations.KeepEmptySpaceCalls);
        Assert.HasCount(2, store.WrittenConfiguration!.Spaces);
        ConfluenceSpaceConfiguration created = store.WrittenConfiguration.Spaces[1];
        Assert.AreEqual("ANSSIWS", created.SpaceKey);
        Assert.AreEqual("confluence/ANSSIWS", created.Target);
        Assert.AreEqual("pro-confidentiel", created.Classification);
        CollectionAssert.AreEqual(AnssiwsPage, created.PageIds.ToArray());
        Assert.AreEqual("DOC", store.WrittenConfiguration.Spaces[0].SpaceKey);
        Assert.AreEqual("docs", store.WrittenConfiguration.Spaces[0].Target);
    }

    [TestMethod]
    public async Task ACancelledPageChoiceRemovesTheSpaceItJustAllowlisted()
    {
        FakeConfigStore store = new(PagesSnapshot()) { ReflectWrites = true };
        FakeConfirmations confirmations = new()
        {
            AddSpaceAccepted = true,
            AddAccepted = false,
            KeepEmptySpaceAccepted = false,
        };
        PagesMutationService service = new(AnssiwsCliClient(), store, confirmations);

        bool added = await service.AddSpaceAsync(
            "https://raw.example.test/spaces/ANSSIWS/pages/1683736048/ADSEC+Platform",
            "pro-confidentiel",
            false,
            CancellationToken.None);

        Assert.IsFalse(added);
        Assert.AreEqual(1, confirmations.KeepEmptySpaceCalls);
        Assert.HasCount(1, store.WrittenConfiguration!.Spaces);
        Assert.AreEqual("DOC", store.WrittenConfiguration.Spaces[0].SpaceKey);
    }

    [TestMethod]
    public async Task AnEmptySpaceSurvivesWhenTheUserChoosesToKeepIt()
    {
        FakeConfigStore store = new(PagesSnapshot()) { ReflectWrites = true };
        FakeConfirmations confirmations = new()
        {
            AddSpaceAccepted = true,
            AddAccepted = false,
            KeepEmptySpaceAccepted = true,
        };
        PagesMutationService service = new(AnssiwsCliClient(), store, confirmations);

        Assert.IsFalse(await service.AddSpaceAsync(
            "https://raw.example.test/spaces/ANSSIWS/pages/1683736048/ADSEC+Platform",
            "pro-confidentiel",
            false,
            CancellationToken.None));

        Assert.AreEqual(1, confirmations.KeepEmptySpaceCalls);
        Assert.HasCount(2, store.WrittenConfiguration!.Spaces);
        Assert.IsEmpty(store.WrittenConfiguration.Spaces[1].PageIds);
    }

    [TestMethod]
    public async Task RemovingTheLastPageAsksBeforeLeavingTheSpaceEmpty()
    {
        FakeConfigStore store = new(SelectionSnapshot(ConfluenceSelection.Pages, ["123"]));
        FakeConfirmations confirmations = new() { KeepEmptySpaceAccepted = false };
        PagesMutationService service = new(new FakeCliClient(), store, confirmations);

        bool changed = await service.RemovePageAsync(
            "DOC",
            "123",
            "Titre",
            false,
            CancellationToken.None);

        Assert.IsFalse(changed);
        Assert.AreEqual(1, confirmations.KeepEmptySpaceCalls);
        Assert.AreEqual(0, store.WriteCalls);
    }

    private static FakeCliClient AnssiwsCliClient() => new()
    {
        ResolveResult = Success(new ResolvedPageContract
        {
            ContractVersion = 1,
            PageId = "1683736048",
            Title = "ADSEC Platform",
            SpaceKey = "ANSSIWS",
            Configured = false,
        }),
    };

    [TestMethod]
    public async Task AddSpaceRejectsAnAlreadyAllowlistedSpace()
    {
        FakeConfigStore store = new(PagesSnapshot());
        PagesMutationService service = new(
            new FakeCliClient(),
            store,
            new FakeConfirmations { AddSpaceAccepted = true });

        PageMutationRejectedException exception = await Assert.ThrowsAsync<PageMutationRejectedException>(() =>
            service.AddSpaceAsync(
                "https://raw.example.test/spaces/doc/pages/1/T",
                "pro-confidentiel",
                false,
                CancellationToken.None));

        Assert.AreEqual(UiStrings.PagesRejectSpaceAlreadyAllowlisted, exception.Message);
        Assert.AreEqual(0, store.WriteCalls);
    }

    [TestMethod]
    public async Task AddSpaceRejectsAReferenceThatCarriesNoSpaceKey()
    {
        FakeConfigStore store = new(PagesSnapshot());
        PagesMutationService service = new(
            new FakeCliClient(),
            store,
            new FakeConfirmations { AddSpaceAccepted = true });

        PageMutationRejectedException exception = await Assert.ThrowsAsync<PageMutationRejectedException>(() =>
            service.AddSpaceAsync(
                "https://raw.example.test/pages/viewpage.action?pageId=1683736048",
                "pro-confidentiel",
                false,
                CancellationToken.None));

        Assert.AreEqual(UiStrings.PagesRejectSpaceKeyNotInferable, exception.Message);
        Assert.AreEqual(0, store.ReadCalls);
        Assert.AreEqual(0, store.WriteCalls);
    }

    [TestMethod]
    public async Task AddSpaceRejectsAReferenceFromAnotherConfluenceServer()
    {
        FakeConfigStore store = new(PagesSnapshot());
        PagesMutationService service = new(
            new FakeCliClient(),
            store,
            new FakeConfirmations { AddSpaceAccepted = true });

        PageMutationRejectedException exception = await Assert.ThrowsAsync<PageMutationRejectedException>(() =>
            service.AddSpaceAsync(
                "https://other.example.test/spaces/ANSSIWS/pages/1/T",
                "pro-confidentiel",
                false,
                CancellationToken.None));

        Assert.AreEqual(UiStrings.PagesRejectSpaceForeignBaseUrl, exception.Message);
        Assert.AreEqual(0, store.WriteCalls);
    }

    [TestMethod]
    public async Task DeclinedSpaceConfirmationWritesNothing()
    {
        FakeConfigStore store = new(PagesSnapshot());
        PagesMutationService service = new(
            new FakeCliClient(),
            store,
            new FakeConfirmations { AddSpaceAccepted = false });

        bool added = await service.AddSpaceAsync(
            "https://raw.example.test/spaces/ANSSIWS/pages/1/T",
            "pro-confidentiel",
            false,
            CancellationToken.None);

        Assert.IsFalse(added);
        Assert.AreEqual(0, store.WriteCalls);
    }

    [TestMethod]
    public async Task AddSpaceRefusesInReadOnlyMode()
    {
        FakeConfigStore store = new(PagesSnapshot());
        PagesMutationService service = new(
            new FakeCliClient(),
            store,
            new FakeConfirmations { AddSpaceAccepted = true });

        await Assert.ThrowsAsync<PageMutationRejectedException>(() =>
            service.AddSpaceAsync(
                "https://raw.example.test/spaces/ANSSIWS/pages/1/T",
                "pro-confidentiel",
                true,
                CancellationToken.None));

        Assert.AreEqual(0, store.ReadCalls);
    }

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

    private static ConfluenceConfigSnapshot SelectionSnapshot(
        ConfluenceSelection selection,
        IReadOnlyList<string> pageIds,
        int schemaVersion = 2) => Snapshot(
        new ConfluenceConfiguration(
            schemaVersion,
            "https://raw.example.test",
            "raw-target",
            null,
            null,
            50,
            0.1,
            [new ConfluenceSpaceConfiguration("DOC", "docs", "pro-confidentiel", selection, pageIds)]));

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

        public Task<ConfluenceCliResult<ScopePreviewContract>> PreviewAsync(
            string reference,
            CancellationToken cancellationToken)
        {
            ResolveCalls++;
            ResolvedPageContract? resolved = ResolveResult.Value;
            ScopePreviewContract? preview = resolved is null
                ? null
                : new ScopePreviewContract
                {
                    ContractVersion = 1,
                    PageId = resolved.PageId,
                    Title = resolved.Title,
                    SpaceKey = resolved.SpaceKey,
                    RecommendedSelection = "subtree",
                    PageOnly = new ScopeChoiceContract { PageCount = 1, EstimatedBytes = 393_216 },
                    Subtree = new ScopeChoiceContract { PageCount = 12, EstimatedBytes = 4_718_592 },
                    WholeSpace = new ScopeChoiceContract { PageCount = 20, EstimatedBytes = 7_864_320 },
                    StorageRoot = "C:\\state",
                    RetentionGenerations = 2,
                };
            return Task.FromResult(new ConfluenceCliResult<ScopePreviewContract>(
                ResolveResult.ExitCode,
                preview,
                ResolveResult.StandardError,
                ResolveResult.TimedOut,
                ResolveResult.LaunchError));
        }
    }

    private sealed class FakeConfigStore(ConfluenceConfigSnapshot snapshot) : IConfluenceConfigStore
    {
        public bool ConflictOnWrite { get; init; }

        public bool ReflectWrites { get; init; }

        public ConfluenceConfigSnapshot? ReloadedSnapshot { get; init; }

        public int ReadCalls { get; private set; }

        public int WriteCalls { get; private set; }

        public ConfluenceConfiguration? WrittenConfiguration { get; private set; }

        public Task<ConfluenceConfigSnapshot> ReadAsync(CancellationToken cancellationToken)
        {
            ReadCalls++;
            if (ReflectWrites && WrittenConfiguration is not null)
            {
                return Task.FromResult(new ConfluenceConfigSnapshot(
                    snapshot.Content,
                    snapshot.ContentHash,
                    WrittenConfiguration));
            }

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

        public ConfluenceSelection SelectedScope { get; init; } = ConfluenceSelection.Pages;

        public string? TypedValue { get; init; }

        public ResolvedPageContract? LastResolvedPage { get; private set; }

        public bool AddSpaceAccepted { get; init; }

        public string? LastSpaceKey { get; private set; }

        public string? LastClassification { get; private set; }

        public bool ConfirmAdd(ResolvedPageContract page)
        {
            LastResolvedPage = page;
            return AddAccepted;
        }

        public bool KeepEmptySpaceAccepted { get; init; }

        public int KeepEmptySpaceCalls { get; private set; }

        public bool ConfirmKeepEmptySpace(string spaceKey)
        {
            KeepEmptySpaceCalls++;
            LastSpaceKey = spaceKey;
            return KeepEmptySpaceAccepted;
        }

        public bool ConfirmAddSpace(string spaceKey, string classification)
        {
            LastSpaceKey = spaceKey;
            LastClassification = classification;
            return AddSpaceAccepted;
        }

        public ConfluenceSelection? ChooseScope(ScopePreviewContract preview)
        {
            LastResolvedPage = new ResolvedPageContract
            {
                ContractVersion = preview.ContractVersion,
                PageId = preview.PageId,
                Title = preview.Title,
                SpaceKey = preview.SpaceKey,
                Configured = false,
            };
            return AddAccepted ? SelectedScope : null;
        }

        public bool ConfirmRemove(string spaceKey, string pageId, string? title) => true;

        public ConfluenceSelection? LastTargetSelection { get; private set; }

        public IReadOnlyList<string>? LastTargetPageIds { get; private set; }

        public string? ConfirmModeChange(
            string spaceKey,
            ConfluenceSelection targetSelection,
            IReadOnlyList<string> targetPageIds)
        {
            LastTargetSelection = targetSelection;
            LastTargetPageIds = targetPageIds;
            return TypedValue;
        }
    }
}
