// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Models;
using CortexCompanion.Tests.Services;
using CortexCompanion.Tests.TestSupport;
using CortexCompanion.ViewModels;

namespace CortexCompanion.Tests.ViewModels;

[TestClass]
public sealed class SyncViewModelTests
{
    [TestMethod]
    public async Task ReadOnlyHandshakeKeepsDirectHealthVisibleAndDisablesBothActions()
    {
        using TemporaryDirectory temporary = new();
        string cliPath = temporary.CreateFakeCli();
        string configPath = CreateConfig(temporary);
        string healthPath = CreateHealth(temporary, "sync_already_running");
        byte[] before = SHA256.HashData(File.ReadAllBytes(healthPath));
        StubSyncRunCoordinator coordinator = new();
        StubInteractiveLauncher interactive = new();
        SyncViewModel viewModel = CreateViewModel(
            cliPath,
            configPath,
            healthPath,
            coordinator,
            interactive);

        await viewModel.InitializeAsync(isReadOnly: true, CancellationToken.None);

        Assert.IsTrue(viewModel.HasHealth);
        Assert.AreEqual(UiStrings.SyncHealthLockedInformation, viewModel.HealthStatus);
        Assert.IsFalse(viewModel.CanRunActions);
        Assert.IsFalse(viewModel.SyncCommand.CanExecute(null));
        Assert.IsFalse(viewModel.StoreCredentialCommand.CanExecute(null));
        CollectionAssert.AreEqual(before, SHA256.HashData(File.ReadAllBytes(healthPath)));
    }

    [TestMethod]
    public async Task SyncCommandIsNonReentrantWhileWorkerLaunchIsPending()
    {
        using TemporaryDirectory temporary = new();
        string cliPath = temporary.CreateFakeCli();
        string configPath = CreateConfig(temporary);
        string healthPath = Path.Combine(temporary.Path, "source-health.json");
        BlockingSyncRunCoordinator coordinator = new();
        SyncViewModel viewModel = CreateViewModel(
            cliPath,
            configPath,
            healthPath,
            coordinator,
            new StubInteractiveLauncher());
        await viewModel.InitializeAsync(isReadOnly: false, CancellationToken.None);

        viewModel.SyncCommand.Execute(null);
        await coordinator.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        viewModel.SyncCommand.Execute(null);

        Assert.AreEqual(1, coordinator.StartCallCount);
        Assert.IsFalse(viewModel.SyncCommand.CanExecute(null));
        coordinator.Release.TrySetResult(coordinator.Handle);
        await WaitUntilAsync(() => viewModel.RunResult == UiStrings.SyncSucceeded);
        Assert.AreEqual(1, coordinator.StartCallCount);
        Assert.AreEqual(UiStrings.LocalSyncRunTitle, viewModel.RunTitle);
    }

    [TestMethod]
    public async Task CredentialExitZeroMakesNoValidityClaim()
    {
        using TemporaryDirectory temporary = new();
        string cliPath = temporary.CreateFakeCli();
        string configPath = CreateConfig(temporary);
        string healthPath = CreateHealth(temporary, "remote_failure");
        StubInteractiveLauncher interactive = new()
        {
            Result = new InteractiveProcessResult(0, null),
            BeforeReturn = () => File.WriteAllText(
                healthPath,
                IngestionHealthReaderTests.ValidJson("ok", null)),
        };
        SyncViewModel viewModel = CreateViewModel(
            cliPath,
            configPath,
            healthPath,
            new StubSyncRunCoordinator(),
            interactive);
        await viewModel.InitializeAsync(isReadOnly: false, CancellationToken.None);
        Assert.AreEqual(UiStrings.SyncHealthError, viewModel.HealthStatus);

        viewModel.StoreCredentialCommand.Execute(null);
        await WaitUntilAsync(() =>
            viewModel.StateMessage == UiStrings.CredentialStored &&
            viewModel.HealthStatus == UiStrings.SyncHealthOk);

        Assert.AreEqual(1, interactive.CallCount);
        CollectionAssert.AreEqual(
            new[] { "confluence", "--config", Path.GetFullPath(configPath), "store-credential" },
            interactive.LastArguments!.ToArray());
        StringAssert.Contains(viewModel.StateMessage, "prochain sync");
    }

    [TestMethod]
    public async Task ManifestPresentationCoversMissingUnreadableOkAndError()
    {
        using TemporaryDirectory temporary = new();
        string healthPath = Path.Combine(temporary.Path, "source-health.json");
        SyncViewModel viewModel = CreateViewModel(
            temporary.CreateFakeCli(),
            CreateConfig(temporary),
            healthPath,
            new StubSyncRunCoordinator(),
            new StubInteractiveLauncher());

        await viewModel.InitializeAsync(isReadOnly: false, CancellationToken.None);
        Assert.AreEqual(UiStrings.SyncNeverRun, viewModel.HealthStatus);
        Assert.IsFalse(viewModel.HasHealth);

        File.WriteAllText(healthPath, "{");
        viewModel.RefreshCommand.Execute(null);
        await WaitUntilAsync(() =>
            viewModel.HealthStatus == UiStrings.SyncHealthUnreadable &&
            !viewModel.IsBusy &&
            viewModel.RefreshCommand.CanExecute(null));
        Assert.IsFalse(viewModel.HasHealth);

        File.WriteAllText(healthPath, IngestionHealthReaderTests.ValidJson("ok", null));
        viewModel.RefreshCommand.Execute(null);
        await WaitUntilAsync(() =>
            viewModel.HealthStatus == UiStrings.SyncHealthOk &&
            !viewModel.IsBusy &&
            viewModel.RefreshCommand.CanExecute(null));
        Assert.IsTrue(viewModel.HasHealth);

        File.WriteAllText(healthPath, IngestionHealthReaderTests.ValidJson("error", "remote_failure"));
        viewModel.RefreshCommand.Execute(null);
        await WaitUntilAsync(() =>
            viewModel.HealthStatus == UiStrings.SyncHealthError &&
            !viewModel.IsBusy &&
            viewModel.RefreshCommand.CanExecute(null));
        Assert.AreEqual("remote_failure", viewModel.ErrorCode);
    }

    [TestMethod]
    public void EmptyEnvironmentOverridesHaveOneExplicitVisibilityState()
    {
        using TemporaryDirectory temporary = new();
        SyncViewModel viewModel = CreateViewModel(
            temporary.CreateFakeCli(),
            CreateConfig(temporary),
            Path.Combine(temporary.Path, "source-health.json"),
            new StubSyncRunCoordinator(),
            new StubInteractiveLauncher());

        Assert.IsFalse(viewModel.HasOverrides);
    }

    [TestMethod]
    public async Task MissingConfluenceConfigurationKeepsLocalSyncActionable()
    {
        using TemporaryDirectory temporary = new();
        SyncViewModel viewModel = CreateViewModel(
            temporary.CreateFakeCli(),
            Path.Combine(temporary.Path, "missing.toml"),
            Path.Combine(temporary.Path, "source-health.json"),
            new StubSyncRunCoordinator(),
            new StubInteractiveLauncher());

        await viewModel.InitializeAsync(isReadOnly: false, CancellationToken.None);

        Assert.IsTrue(viewModel.CanRunLocalDocuments);
        Assert.IsTrue(viewModel.SyncCommand.CanExecute(null));
        Assert.IsFalse(viewModel.CanRunConfluenceActions);
        Assert.IsFalse(viewModel.ConfluenceSyncCommand.CanExecute(null));
        Assert.IsFalse(viewModel.StoreCredentialCommand.CanExecute(null));
    }

    [TestMethod]
    [DataRow(0, "Synchronisation terminée")]
    [DataRow(1, "code 1")]
    [DataRow(2, "code 2")]
    [DataRow(3, "code 3")]
    [DataRow(4, "code 4")]
    [DataRow(5, "code 5")]
    [DataRow(6, "code non nominal 6")]
    [DataRow(7, "code non nominal 7")]
    [DataRow(8, "code non nominal 8")]
    public async Task LatestRunMapsCompleteFrozenExitTable(int exitCode, string expectedText)
    {
        using TemporaryDirectory temporary = new();
        string cliPath = temporary.CreateFakeCli();
        string configPath = CreateConfig(temporary);
        SyncRunHandle handle = new("run", temporary.Path, Environment.ProcessId, DateTimeOffset.UtcNow);
        LatestRunCoordinator coordinator = new(new SyncRunSnapshot(
            handle,
            string.Empty,
            string.Empty,
            false,
            true,
            false,
            exitCode,
            null));
        SyncViewModel viewModel = CreateViewModel(
            cliPath,
            configPath,
            Path.Combine(temporary.Path, "source-health.json"),
            coordinator,
            new StubInteractiveLauncher());

        await viewModel.InitializeAsync(isReadOnly: false, CancellationToken.None);

        StringAssert.Contains(viewModel.RunResult, expectedText);
        Assert.AreEqual(UiStrings.ConfluenceSyncRunTitle, viewModel.RunTitle);
    }

    [TestMethod]
    public async Task LockedResultDoesNotRetryAndNeverWritesConfluenceToml()
    {
        using TemporaryDirectory temporary = new();
        string cliPath = temporary.CreateFakeCli();
        string configPath = CreateConfig(temporary);
        byte[] before = SHA256.HashData(File.ReadAllBytes(configPath));
        ImmediateRunCoordinator coordinator = new(2);
        SyncViewModel viewModel = CreateViewModel(
            cliPath,
            configPath,
            Path.Combine(temporary.Path, "source-health.json"),
            coordinator,
            new StubInteractiveLauncher());
        await viewModel.InitializeAsync(isReadOnly: false, CancellationToken.None);

        viewModel.SyncCommand.Execute(null);
        await WaitUntilAsync(() =>
            viewModel.RunResult == UiStrings.SyncLocked &&
            viewModel.StateMessage == UiStrings.SyncStateReady &&
            !viewModel.IsBusy);

        Assert.AreEqual(1, coordinator.StartCallCount);
        CollectionAssert.AreEqual(before, SHA256.HashData(File.ReadAllBytes(configPath)));
    }

    private static SyncViewModel CreateViewModel(
        string cliPath,
        string configPath,
        string healthPath,
        ISyncRunCoordinator coordinator,
        IInteractiveProcessLauncher interactive) =>
        new(
            coordinator,
            interactive,
            cliPath,
            configPath,
            new IngestionPathResolution(
                Path.Combine(Path.GetDirectoryName(healthPath)!, "ingestion.toml"),
                IngestionPathOrigin.Default,
                "APPDATA",
                Path.GetDirectoryName(Path.GetDirectoryName(healthPath)!)!,
                IngestionPathOrigin.Default,
                "LOCALAPPDATA",
                healthPath),
            []);

    private static string CreateConfig(TemporaryDirectory temporary)
    {
        string configPath = Path.Combine(temporary.Path, "confluence.toml");
        File.WriteAllText(configPath, "schema_version = 1\nauth_expires_at = 2026-11-01T00:00:00+01:00\n");
        return configPath;
    }

    private static string CreateHealth(TemporaryDirectory temporary, string? errorCode)
    {
        string healthPath = Path.Combine(temporary.Path, "source-health.json");
        File.WriteAllText(healthPath, IngestionHealthReaderTests.ValidJson("error", errorCode));
        return healthPath;
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(2);
        while (!condition())
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                Assert.Fail("The asynchronous view-model condition was not reached.");
            }

            await Task.Delay(10);
        }
    }

    private sealed class StubSyncRunCoordinator : ISyncRunCoordinator
    {
        public Task<SyncRunHandle> StartLocalDocumentsAsync(
            string cliPath,
            CancellationToken cancellationToken) =>
            Task.FromException<SyncRunHandle>(new AssertFailedException("Unexpected local sync launch."));

        public Task<SyncRunHandle> StartConfluenceAsync(
            string cliPath,
            string confluenceConfigPath,
            CancellationToken cancellationToken) =>
            Task.FromException<SyncRunHandle>(new AssertFailedException("Unexpected Confluence sync launch."));

        public Task<SyncRunSnapshot?> GetLatestAsync(CancellationToken cancellationToken) =>
            Task.FromResult<SyncRunSnapshot?>(null);

        public Task<SyncRunSnapshot> ObserveAsync(
            SyncRunHandle handle,
            CancellationToken cancellationToken) =>
            Task.FromException<SyncRunSnapshot>(new AssertFailedException("Unexpected sync observation."));
    }

    private sealed class BlockingSyncRunCoordinator : ISyncRunCoordinator
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<SyncRunHandle> Release { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public SyncRunHandle Handle { get; } = new(
            "run",
            Path.Combine(Path.GetTempPath(), "run"),
            Environment.ProcessId,
            DateTimeOffset.UtcNow,
            SyncRunKind.LocalDocuments);
        public int StartCallCount { get; private set; }

        public async Task<SyncRunHandle> StartLocalDocumentsAsync(
            string cliPath,
            CancellationToken cancellationToken)
        {
            StartCallCount++;
            Started.TrySetResult();
            return await Release.Task.WaitAsync(cancellationToken);
        }

        public Task<SyncRunHandle> StartConfluenceAsync(
            string cliPath,
            string confluenceConfigPath,
            CancellationToken cancellationToken) =>
            Task.FromException<SyncRunHandle>(new AssertFailedException("Unexpected Confluence sync launch."));

        public Task<SyncRunSnapshot?> GetLatestAsync(CancellationToken cancellationToken) =>
            Task.FromResult<SyncRunSnapshot?>(null);

        public Task<SyncRunSnapshot> ObserveAsync(
            SyncRunHandle handle,
            CancellationToken cancellationToken) =>
            Task.FromResult(new SyncRunSnapshot(
                handle,
                string.Empty,
                "{}",
                false,
                true,
                false,
                0,
                null));
    }

    private sealed class LatestRunCoordinator(SyncRunSnapshot latest) : ISyncRunCoordinator
    {
        public Task<SyncRunHandle> StartLocalDocumentsAsync(
            string cliPath,
            CancellationToken cancellationToken) =>
            Task.FromException<SyncRunHandle>(new AssertFailedException("Unexpected local sync launch."));

        public Task<SyncRunHandle> StartConfluenceAsync(
            string cliPath,
            string confluenceConfigPath,
            CancellationToken cancellationToken) =>
            Task.FromException<SyncRunHandle>(new AssertFailedException("Unexpected Confluence sync launch."));

        public Task<SyncRunSnapshot?> GetLatestAsync(CancellationToken cancellationToken) =>
            Task.FromResult<SyncRunSnapshot?>(latest);

        public Task<SyncRunSnapshot> ObserveAsync(
            SyncRunHandle handle,
            CancellationToken cancellationToken) =>
            Task.FromResult(latest);
    }

    private sealed class ImmediateRunCoordinator(int exitCode) : ISyncRunCoordinator
    {
        private readonly SyncRunHandle _handle = new(
            "run",
            Path.Combine(Path.GetTempPath(), "run"),
            Environment.ProcessId,
            DateTimeOffset.UtcNow,
            SyncRunKind.LocalDocuments);

        public int StartCallCount { get; private set; }

        public Task<SyncRunHandle> StartLocalDocumentsAsync(
            string cliPath,
            CancellationToken cancellationToken)
        {
            StartCallCount++;
            return Task.FromResult(_handle);
        }

        public Task<SyncRunHandle> StartConfluenceAsync(
            string cliPath,
            string confluenceConfigPath,
            CancellationToken cancellationToken) =>
            Task.FromException<SyncRunHandle>(new AssertFailedException("Unexpected Confluence sync launch."));

        public Task<SyncRunSnapshot?> GetLatestAsync(CancellationToken cancellationToken) =>
            Task.FromResult<SyncRunSnapshot?>(null);

        public Task<SyncRunSnapshot> ObserveAsync(
            SyncRunHandle handle,
            CancellationToken cancellationToken) =>
            Task.FromResult(new SyncRunSnapshot(
                handle,
                string.Empty,
                string.Empty,
                false,
                true,
                false,
                exitCode,
                null));
    }

    private sealed class StubInteractiveLauncher : IInteractiveProcessLauncher
    {
        public InteractiveProcessResult Result { get; init; } = new(1, null);
        public Action? BeforeReturn { get; init; }
        public int CallCount { get; private set; }
        public IReadOnlyList<string>? LastArguments { get; private set; }

        public Task<InteractiveProcessResult> RunAsync(
            string filePath,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastArguments = arguments;
            BeforeReturn?.Invoke();
            return Task.FromResult(Result);
        }
    }
}
