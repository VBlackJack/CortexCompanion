// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Commands;
using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;
using CortexCompanion.ViewModels;

namespace CortexCompanion.Tests.ViewModels;

[TestClass]
public sealed class PagesViewModelTests
{
    private const string SnapshotHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [TestMethod]
    public async Task MissingConfigurationExplainsPrerequisiteAndDisablesPageMutation()
    {
        using TemporaryDirectory temporary = new();
        string configPath = Path.Combine(temporary.Path, "missing.toml");
        StubCliClient cliClient = new();
        PagesMutationService mutations = new(
            cliClient,
            new StubConfigStore(),
            new RejectingConfirmationService());
        PagesViewModel viewModel = new(
            cliClient,
            mutations,
            null,
            null,
            new ConfluenceConfigPathResolution(
                configPath,
                ConfluenceConfigPathOrigin.Default,
                "APPDATA"),
            []);

        await viewModel.InitializeAsync(isReadOnly: false);

        Assert.IsFalse(viewModel.HasConfluenceConfiguration);
        Assert.IsFalse(viewModel.CanMutate);
        Assert.AreEqual(UiStrings.PagesConfigurationRequired, viewModel.StateMessage);
        Assert.AreEqual(0, cliClient.GetPagesCount);
    }

    [TestMethod]
    public async Task RefreshEnablesPageMutationAfterConfigurationAppears()
    {
        using TemporaryDirectory temporary = new();
        string configPath = Path.Combine(temporary.Path, "confluence.toml");
        StubCliClient cliClient = new();
        PagesMutationService mutations = new(
            cliClient,
            new StubConfigStore(),
            new RejectingConfirmationService());
        PagesViewModel viewModel = new(
            cliClient,
            mutations,
            null,
            null,
            new ConfluenceConfigPathResolution(
                configPath,
                ConfluenceConfigPathOrigin.Default,
                "APPDATA"),
            []);
        await viewModel.InitializeAsync(isReadOnly: false);
        await File.WriteAllTextAsync(configPath, "schema_version = 1\n");

        await ((AsyncRelayCommand)viewModel.RefreshCommand).ExecuteAsync(parameter: null);

        Assert.IsTrue(viewModel.HasConfluenceConfiguration);
        Assert.IsTrue(viewModel.CanMutate);
        Assert.AreEqual(UiStrings.PagesNoSpaces, viewModel.StateMessage);
        Assert.AreEqual(1, cliClient.GetPagesCount);
    }

    [TestMethod]
    public async Task RefreshTimeoutExplainsWhereToIncreaseTheSharedCliLimit()
    {
        using TemporaryDirectory temporary = new();
        string configPath = Path.Combine(temporary.Path, "confluence.toml");
        await File.WriteAllTextAsync(configPath, "schema_version = 2\n");
        StubCliClient cliClient = new()
        {
            PagesResult = new ConfluenceCliResult<PagesContract>(
                CortexExitCode.Error,
                null,
                string.Empty,
                true,
                null),
        };
        PagesViewModel viewModel = new(
            cliClient,
            new PagesMutationService(
                cliClient,
                new StubConfigStore(),
                new RejectingConfirmationService()),
            null,
            null,
            new ConfluenceConfigPathResolution(
                configPath,
                ConfluenceConfigPathOrigin.Default,
                "APPDATA"),
            []);

        await viewModel.InitializeAsync(isReadOnly: false);

        Assert.AreEqual(UiStrings.PagesCliTimedOut, viewModel.StateMessage);
        Assert.Contains("Réglages", viewModel.StateMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("refusé", viewModel.StateMessage, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task FirstRunInfersSpaceCreatesConfigurationAndAddsConfirmedPage()
    {
        using TemporaryDirectory temporary = new();
        string configPath = Path.Combine(temporary.Path, "confluence.toml");
        string converterPath = Path.Combine(temporary.Path, "ConfluenceRAGBuilder.Console.exe");
        await File.WriteAllBytesAsync(converterPath, [0x4d, 0x5a]);
        StubCliClient cliClient = new();
        ConfluenceConfigStore store = new(configPath);
        PagesMutationService mutations = new(
            cliClient,
            store,
            new AcceptingConfirmationService());
        PagesViewModel viewModel = new(
            cliClient,
            mutations,
            new ConfluenceSetupService(
                store,
                new ConfluenceConverterProbe(new StubProcessRunner(ProcessRunResult.Completed(
                    0,
                    "{\"tool_version\":\"1.2.0\",\"schema_version\":1}",
                    string.Empty))),
                converterPath),
            null,
            new ConfluenceConfigPathResolution(
                configPath,
                ConfluenceConfigPathOrigin.Default,
                "APPDATA"),
            []);
        await viewModel.InitializeAsync(isReadOnly: false);

        viewModel.SetupPageUrl = "https://kazan.example.test/wiki/spaces/DOC/pages/1001/Run+Book";
        viewModel.SetupExpiryDate = new DateTime(2099, 12, 31);

        Assert.AreEqual("DOC", viewModel.SetupSpaceKey);
        Assert.IsTrue(viewModel.CanInitializeConfluence);
        await ((AsyncRelayCommand)viewModel.InitializeConfluenceCommand).ExecuteAsync(parameter: null);

        Assert.IsTrue(File.Exists(configPath), viewModel.StateMessage);
        ConfluenceConfigSnapshot snapshot = await store.ReadAsync(CancellationToken.None);
        Assert.IsTrue(viewModel.HasConfluenceConfiguration);
        Assert.IsFalse(viewModel.NeedsConfluenceConfiguration);
        Assert.AreEqual(1, cliClient.ResolveCount);
        Assert.AreEqual("1001", snapshot.Configuration.Spaces[0].PageIds.Single());
        Assert.AreEqual(UiStrings.PagesSetupCompleted, viewModel.StateMessage);
    }

    private sealed class StubCliClient : IConfluenceCliClient
    {
        public ConfluenceCliResult<PagesContract>? PagesResult { get; init; }

        public int GetPagesCount { get; private set; }

        public int ResolveCount { get; private set; }

        public Task<ConfluenceCliResult<PagesContract>> GetPagesAsync(
            CancellationToken cancellationToken)
        {
            GetPagesCount++;
            return Task.FromResult(PagesResult ?? new ConfluenceCliResult<PagesContract>(
                CortexExitCode.Ok,
                new PagesContract
                {
                    ContractVersion = 2,
                    Spaces = [],
                    LastSync = new LastSyncContract(),
                },
                string.Empty,
                false,
                null));
        }

        public Task<ConfluenceCliResult<ResolvedPageContract>> ResolveAsync(
            string reference,
            CancellationToken cancellationToken)
        {
            ResolveCount++;
            return Task.FromResult(new ConfluenceCliResult<ResolvedPageContract>(
                CortexExitCode.Ok,
                new ResolvedPageContract
                {
                    ContractVersion = 1,
                    PageId = "1001",
                    Title = "Run Book",
                    SpaceKey = "DOC",
                    Configured = false,
                },
                string.Empty,
                false,
                null));
        }

        public async Task<ConfluenceCliResult<ScopePreviewContract>> PreviewAsync(
            string reference,
            CancellationToken cancellationToken)
        {
            ConfluenceCliResult<ResolvedPageContract> resolved = await ResolveAsync(
                reference,
                cancellationToken);
            return new ConfluenceCliResult<ScopePreviewContract>(
                resolved.ExitCode,
                new ScopePreviewContract
                {
                    ContractVersion = 1,
                    PageId = "1001",
                    Title = "Run Book",
                    SpaceKey = "DOC",
                    RecommendedSelection = "subtree",
                    PageOnly = new ScopeChoiceContract { PageCount = 1, EstimatedBytes = 393_216 },
                    Subtree = new ScopeChoiceContract { PageCount = 8, EstimatedBytes = 3_145_728 },
                    WholeSpace = new ScopeChoiceContract { PageCount = 10, EstimatedBytes = 3_932_160 },
                    StorageRoot = "C:\\state",
                    RetentionGenerations = 2,
                },
                string.Empty,
                false,
                null);
        }
    }

    private sealed class StubConfigStore : IConfluenceConfigStore
    {
        public Task<ConfluenceConfigSnapshot> ReadAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Config reads are outside this read-state test.");

        public Task<ConfluenceConfigSnapshot> WriteAsync(
            ConfluenceConfiguration configuration,
            string? expectedHash,
            CancellationToken cancellationToken) =>
            Task.FromResult(new ConfluenceConfigSnapshot([], SnapshotHash, configuration));
    }

    private sealed class RejectingConfirmationService : IPageMutationConfirmationService
    {
        public bool ConfirmAdd(ResolvedPageContract page) => false;

        public ConfluenceSelection? ChooseScope(ScopePreviewContract preview) => null;

        public bool ConfirmRemove(string spaceKey, string pageId, string? title) => false;

        public string? ConfirmModeChange(
            string spaceKey,
            ConfluenceSelection targetSelection,
            IReadOnlyList<string> targetPageIds) => null;
    }

    private sealed class AcceptingConfirmationService : IPageMutationConfirmationService
    {
        public bool ConfirmAdd(ResolvedPageContract page) => true;

        public ConfluenceSelection? ChooseScope(ScopePreviewContract preview) =>
            ConfluenceSelection.Subtree;

        public bool ConfirmRemove(string spaceKey, string pageId, string? title) => true;

        public string? ConfirmModeChange(
            string spaceKey,
            ConfluenceSelection targetSelection,
            IReadOnlyList<string> targetPageIds) => spaceKey;
    }
}
