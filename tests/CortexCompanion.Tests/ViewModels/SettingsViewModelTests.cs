// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Commands;
using CortexCompanion.Constants;
using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;
using CortexCompanion.ViewModels;

namespace CortexCompanion.Tests.ViewModels;

[TestClass]
public sealed class SettingsViewModelTests
{
    private const string SnapshotHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private static readonly int[] ExpectedHandshakeTimeoutOptions = [15, 30, 60, 120];

    [TestMethod]
    public async Task SaveCliDoesNotPersistOrReportSuccessWhenReplacementCompositionFails()
    {
        using TemporaryDirectory temporary = new();
        string cliA = temporary.CreateFakeCli();
        string replacementDirectory = Path.Combine(temporary.Path, "replacement");
        string cliB = Path.Combine(replacementDirectory, "cortex.exe");
        Directory.CreateDirectory(replacementDirectory);
        File.WriteAllText(cliB, string.Empty);
        TestContext context = await CreateInitializedContextAsync(temporary, cliA);
        context.Coordinator.FailPath = cliB;

        context.ViewModel.CliPath = cliB;
        await ExecuteAsync(context.ViewModel.SaveCliCommand);

        SettingsLoadResult stored = await context.SettingsStore.LoadAsync();
        Assert.AreEqual(cliA, stored.Settings.CliPath);
        Assert.AreEqual(cliA, context.ViewModel.CliPath);
        Assert.AreEqual(UiStrings.SettingsCliReplacementFailedPreviousRetained, context.ViewModel.StatusMessage);
        Assert.AreNotEqual(UiStrings.SettingsCliSaved, context.ViewModel.StatusMessage);
    }

    [TestMethod]
    public async Task SaveCliAppliesAndPersistsSelectedHandshakeTimeout()
    {
        using TemporaryDirectory temporary = new();
        string cliPath = temporary.CreateFakeCli();
        TestContext context = await CreateInitializedContextAsync(temporary, cliPath);
        CollectionAssert.AreEqual(
            ExpectedHandshakeTimeoutOptions,
            context.ViewModel.CliHandshakeTimeoutOptions.ToArray());
        Assert.AreEqual(
            AppConstants.DefaultCliHandshakeTimeoutSeconds,
            context.ViewModel.CliHandshakeTimeoutSeconds);
        context.ViewModel.CliHandshakeTimeoutSeconds = 120;

        await ExecuteAsync(context.ViewModel.SaveCliCommand);

        SettingsLoadResult stored = await context.SettingsStore.LoadAsync();
        Assert.AreEqual(120, stored.Settings.CliHandshakeTimeoutSeconds);
        Assert.AreEqual(120, context.Coordinator.LastSettings?.CliHandshakeTimeoutSeconds);
        Assert.AreEqual(UiStrings.SettingsCliSaved, context.ViewModel.StatusMessage);
    }

    [TestMethod]
    public async Task RefreshFailureNeverReportsConfigurationRefreshed()
    {
        using TemporaryDirectory temporary = new();
        string cliPath = temporary.CreateFakeCli();
        TestContext context = await CreateInitializedContextAsync(temporary, cliPath);
        context.ConfigClient.GetException = new CortexCliContractException("Simulated invalid response.");

        await ExecuteAsync(context.ViewModel.RefreshCommand);

        Assert.AreEqual(UiStrings.SettingsConfigReadFailed, context.ViewModel.ConfigStateText);
        Assert.AreEqual(UiStrings.SettingsConfigReadFailed, context.ViewModel.StatusMessage);
        Assert.AreNotEqual(UiStrings.SettingsRefreshed, context.ViewModel.StatusMessage);
    }

    [TestMethod]
    public async Task RefreshOutcomeUnknownBlocksSuccessLanguage()
    {
        using TemporaryDirectory temporary = new();
        string cliPath = temporary.CreateFakeCli();
        TestContext context = await CreateInitializedContextAsync(temporary, cliPath);
        context.ConfigClient.GetException = new CortexCliContractException(
            "Simulated unknown outcome.",
            outcomeUnknown: true);

        await ExecuteAsync(context.ViewModel.RefreshCommand);

        Assert.AreEqual(UiStrings.SettingsConfigOutcomeUnknown, context.ViewModel.ConfigStateText);
        Assert.AreEqual(UiStrings.SettingsConfigOutcomeUnknown, context.ViewModel.StatusMessage);
        Assert.AreNotEqual(UiStrings.SettingsRefreshed, context.ViewModel.StatusMessage);
    }

    [TestMethod]
    public async Task StartupFailureNavigatesFromLocalHomeToActionableSettings()
    {
        using TemporaryDirectory temporary = new();
        string cliPath = temporary.CreateFakeCli();
        TestContext context = await CreateInitializedContextAsync(temporary, cliPath);
        MainViewModel main = new(context.Coordinator, context.ViewModel);

        Assert.AreEqual(NavigationPage.LocalKnowledgeBase, main.CurrentPage);

        main.ReportInitializationFailure();

        Assert.AreEqual(NavigationPage.Settings, main.CurrentPage);
        Assert.IsTrue(main.IsSettingsVisible);
        Assert.IsFalse(main.IsInitializing);
        Assert.AreEqual(UiStrings.StartupInitializationError, context.ViewModel.StatusMessage);
    }

    private static async Task<TestContext> CreateInitializedContextAsync(
        TemporaryDirectory temporary,
        string cliPath)
    {
        string settingsPath = Path.Combine(temporary.Path, "state", "settings.json");
        SettingsStore store = new(settingsPath);
        AppSettings settings = new(cliPath);
        await store.SaveAsync(settings);
        CompanionRuntime runtime = CreateCompatibleRuntime(temporary.Path, cliPath);
        TestRuntimeCoordinator coordinator = new(runtime);
        TestConfigClient configClient = new(temporary.Path);
        SettingsViewModel viewModel = new(
            store,
            new CliPathDiscovery(Path.GetDirectoryName(cliPath), temporary.Path),
            coordinator,
            configClient,
            new NullFileDialogs());
        await viewModel.InitializeAsync(new SettingsLoadResult(settings, SettingsLoadState.Loaded));
        return new TestContext(viewModel, store, coordinator, configClient);
    }

    private static CompanionRuntime CreateCompatibleRuntime(string root, string cliPath)
    {
        StubProcessRunner processRunner = new(
            ProcessRunResult.Completed(0, "2026.0808.00", string.Empty));
        CompanionRuntimeFactory factory = new(
            new AppPaths(root),
            new CliHandshakeService(new CliVersionPolicy(), processRunner),
            processRunner);
        CompanionRuntime pending = factory.CreatePending();
        return pending with
        {
            Handshake = new CliHandshakeResult(
                CliHandshakeStatus.Compatible,
                new CliVersion(2026, 8, 8, 0)),
            CliPath = cliPath,
        };
    }

    private static Task ExecuteAsync(System.Windows.Input.ICommand command) =>
        ((AsyncRelayCommand)command).ExecuteAsync(parameter: null);

    private sealed record TestContext(
        SettingsViewModel ViewModel,
        SettingsStore SettingsStore,
        TestRuntimeCoordinator Coordinator,
        TestConfigClient ConfigClient);

    private sealed class TestRuntimeCoordinator(CompanionRuntime current) : ICompanionRuntimeCoordinator
    {
        public event EventHandler<CompanionRuntimeChangedEventArgs>? RuntimeChanged;

        public CompanionRuntime Current { get; private set; } = current;

        public string? FailPath { get; set; }

        public AppSettings? LastSettings { get; private set; }

        public Task<CompanionRuntime> ApplyAsync(
            AppSettings settings,
            CancellationToken cancellationToken = default)
        {
            LastSettings = settings;
            if (string.Equals(settings.CliPath, FailPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Simulated composition failure.");
            }

            Current = Current with { CliPath = settings.CliPath };
            RuntimeChanged?.Invoke(this, new CompanionRuntimeChangedEventArgs(Current));
            return Task.FromResult(Current);
        }
    }

    private sealed class TestConfigClient(string knowledgeBasePath) : ICortexConfigClient
    {
        public CortexCliContractException? GetException { get; set; }

        public Task<CortexConfigSnapshot> GetAsync(
            string cliPath,
            CancellationToken cancellationToken = default) =>
            GetException is null
                ? Task.FromResult(new CortexConfigSnapshot(
                    true,
                    SnapshotHash,
                    true,
                    knowledgeBasePath,
                    null))
                : Task.FromException<CortexConfigSnapshot>(GetException);

        public Task<CortexConfigMutationResult> SetKnowledgeBasePathAsync(
            string cliPath,
            string knowledgeBasePathValue,
            string? expectedContentHash,
            bool expectAbsent,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new CortexConfigMutationResult(
                CortexConfigMutationStatus.Succeeded,
                true,
                SnapshotHash,
                true,
                true,
                null));
    }

    private sealed class NullFileDialogs : IFileDialogService
    {
        public string? SelectCliExecutable(string? currentPath) => null;

        public string? SelectKnowledgeBaseDirectory(string? currentPath) => null;
    }
}
