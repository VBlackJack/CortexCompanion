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

    private sealed class StubCliClient : IConfluenceCliClient
    {
        public int GetPagesCount { get; private set; }

        public Task<ConfluenceCliResult<PagesContract>> GetPagesAsync(
            CancellationToken cancellationToken)
        {
            GetPagesCount++;
            return Task.FromResult(new ConfluenceCliResult<PagesContract>(
                CortexExitCode.Ok,
                new PagesContract
                {
                    ContractVersion = 1,
                    Spaces = [],
                    LastSync = new LastSyncContract(),
                },
                string.Empty,
                false,
                null));
        }

        public Task<ConfluenceCliResult<ResolvedPageContract>> ResolveAsync(
            string reference,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Resolve is outside this read-state test.");
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

        public bool ConfirmRemove(string spaceKey, string pageId, string? title) => false;

        public string? ConfirmModeChange(
            string spaceKey,
            ConfluenceSelection targetSelection,
            IReadOnlyList<string> targetPageIds) => null;
    }
}
