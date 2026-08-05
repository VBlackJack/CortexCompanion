// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Security.Cryptography;
using CortexCompanion.Constants;
using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;
using CortexCompanion.ViewModels;

namespace CortexCompanion.Tests.ViewModels;

[TestClass]
public sealed class SchedulingViewModelTests
{
    [TestMethod]
    public async Task ReadOnlyModeKeepsStateVisibleAndDisablesEveryMutation()
    {
        using TemporaryDirectory temporary = new();
        StubTaskScheduler scheduler = new(ActiveSnapshot(lastResult: 3));
        SchedulingViewModel viewModel = CreateViewModel(temporary, scheduler);

        await viewModel.InitializeAsync(isReadOnly: true, CancellationToken.None);

        Assert.AreEqual(UiStrings.SchedulingStateActive, viewModel.StateText);
        Assert.AreEqual(UiStrings.SchedulingResultNothingToDo, viewModel.LastResultText);
        Assert.IsFalse(viewModel.CanCreateOrUpdate);
        Assert.IsFalse(viewModel.CanDelete);
        Assert.HasCount(1, scheduler.ReadContracts);
    }

    [TestMethod]
    public async Task MissingConfigurationStillReadsStateAndDisablesEveryMutation()
    {
        using TemporaryDirectory temporary = new();
        StubTaskScheduler scheduler = new(ActiveSnapshot(lastResult: 0)) { UseNullContract = true };
        SchedulingViewModel viewModel = CreateViewModel(temporary, scheduler, contract: null);

        await viewModel.InitializeAsync(isReadOnly: false, CancellationToken.None);

        Assert.AreEqual(UiStrings.SchedulingStateActive, viewModel.StateText);
        Assert.IsFalse(viewModel.IsConfigured);
        Assert.IsFalse(viewModel.CanCreateOrUpdate);
        Assert.IsFalse(viewModel.CanDelete);
        Assert.IsNull(scheduler.ReadContracts[0]);
    }

    [TestMethod]
    public async Task BroadEnvironmentBlockShowsNamesAndRemediationButNeverValues()
    {
        using TemporaryDirectory temporary = new();
        string[] blockedNames = ["CORTEX_CONFLUENCE_SECRET_FUTURE", "CORTEX_INGESTION_DATA_ROOT"];
        StubTaskScheduler scheduler = new(ScheduledTaskSnapshot.Absent);
        SchedulingViewModel viewModel = CreateViewModel(
            temporary,
            scheduler,
            blockedNames: blockedNames);

        await viewModel.InitializeAsync(isReadOnly: false, CancellationToken.None);

        Assert.IsTrue(viewModel.HasEnvironmentBlock);
        Assert.IsFalse(viewModel.CanCreateOrUpdate);
        Assert.IsFalse(viewModel.CanDelete);
        StringAssert.Contains(viewModel.EnvironmentBlockMessage, blockedNames[0]);
        StringAssert.Contains(viewModel.EnvironmentBlockMessage, blockedNames[1]);
        StringAssert.Contains(viewModel.EnvironmentBlockMessage, "INGESTION.toml");
        Assert.IsFalse(viewModel.EnvironmentBlockMessage.Contains("secret-value", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task EnvironmentBlockStillAllowsDeletionOfAnOwnedTask()
    {
        using TemporaryDirectory temporary = new();
        StubTaskScheduler scheduler = new(ActiveSnapshot(lastResult: 0));
        SchedulingViewModel viewModel = CreateViewModel(
            temporary,
            scheduler,
            blockedNames: ["CORTEX_INGESTION_DATA_ROOT"]);

        await viewModel.InitializeAsync(isReadOnly: false, CancellationToken.None);

        Assert.IsFalse(viewModel.CanCreateOrUpdate);
        Assert.IsTrue(viewModel.CanDelete);
    }

    [TestMethod]
    public async Task ForeignCollisionRefusesUpdateAndDeleteWithoutCallingMutationBoundary()
    {
        using TemporaryDirectory temporary = new();
        StubTaskScheduler scheduler = new(CollisionSnapshot());
        SchedulingViewModel viewModel = CreateViewModel(temporary, scheduler);
        await viewModel.InitializeAsync(isReadOnly: false, CancellationToken.None);

        viewModel.CreateOrUpdateCommand.Execute(null);
        viewModel.DeleteCommand.Execute(null);
        await Task.Delay(50);

        Assert.AreEqual(UiStrings.SchedulingStateCollision, viewModel.StateText);
        Assert.IsFalse(viewModel.CreateOrUpdateCommand.CanExecute(null));
        Assert.IsFalse(viewModel.DeleteCommand.CanExecute(null));
        Assert.AreEqual(0, scheduler.CreateOrUpdateCallCount);
        Assert.AreEqual(0, scheduler.DeleteCallCount);
    }

    [TestMethod]
    public async Task OwnedDivergentTaskRemainsReconfigurableAndDeletable()
    {
        using TemporaryDirectory temporary = new();
        StubTaskScheduler scheduler = new(ActiveSnapshot(
            lastResult: 0,
            displayState: ScheduledTaskDisplayState.NeedsReconfiguration));
        SchedulingViewModel viewModel = CreateViewModel(temporary, scheduler);

        await viewModel.InitializeAsync(isReadOnly: false, CancellationToken.None);

        Assert.AreEqual(UiStrings.SchedulingStateNeedsReconfiguration, viewModel.StateText);
        Assert.IsTrue(viewModel.CanCreateOrUpdate);
        Assert.IsTrue(viewModel.CanDelete);
    }

    [TestMethod]
    public async Task OwnershipLostBetweenDisplayAndMutationFailsClosed()
    {
        using TemporaryDirectory temporary = new();
        StubTaskScheduler scheduler = new(ActiveSnapshot(lastResult: 0))
        {
            CreateOrUpdateException = new TaskSchedulerCollisionException(),
            DeleteException = new TaskSchedulerCollisionException(),
        };
        SchedulingViewModel viewModel = CreateViewModel(temporary, scheduler);
        await viewModel.InitializeAsync(isReadOnly: false, CancellationToken.None);

        viewModel.CreateOrUpdateCommand.Execute(null);
        await WaitUntilAsync(() => scheduler.CreateOrUpdateAttemptCount == 1);
        viewModel.DeleteCommand.Execute(null);
        await WaitUntilAsync(() => scheduler.DeleteAttemptCount == 1);

        Assert.AreEqual(0, scheduler.CreateOrUpdateCallCount);
        Assert.AreEqual(0, scheduler.DeleteCallCount);
        Assert.AreEqual(UiStrings.SchedulingStateCollision, viewModel.OperationMessage);
    }

    [TestMethod]
    public async Task CreateUsesOneRegistrationAndNeverWritesEitherToml()
    {
        using TemporaryDirectory temporary = new();
        ScheduledTaskContract contract = CreateContract(temporary);
        File.WriteAllText(contract.IngestionConfigPath, "schema_version = 1");
        File.WriteAllText(contract.ConfluenceConfigPath, "schema_version = 1");
        byte[] ingestionBefore = SHA256.HashData(File.ReadAllBytes(contract.IngestionConfigPath));
        byte[] confluenceBefore = SHA256.HashData(File.ReadAllBytes(contract.ConfluenceConfigPath));
        StubTaskScheduler scheduler = new(ScheduledTaskSnapshot.Absent)
        {
            SnapshotAfterMutation = ActiveSnapshot(lastResult: 0),
        };
        SchedulingViewModel viewModel = CreateViewModel(temporary, scheduler, contract);
        await viewModel.InitializeAsync(isReadOnly: false, CancellationToken.None);

        viewModel.CreateOrUpdateCommand.Execute(null);
        await WaitUntilAsync(() => scheduler.CreateOrUpdateCallCount == 1 && !viewModel.IsBusy);

        Assert.IsNotNull(scheduler.LastRegistration);
        Assert.AreEqual(contract.IngestionConfigPath, scheduler.LastRegistration.Contract.IngestionConfigPath);
        CollectionAssert.AreEqual(ingestionBefore, SHA256.HashData(File.ReadAllBytes(contract.IngestionConfigPath)));
        CollectionAssert.AreEqual(confluenceBefore, SHA256.HashData(File.ReadAllBytes(contract.ConfluenceConfigPath)));
    }

    [TestMethod]
    public async Task RunningStateUsesWorkerTimestampFallbackAndNeverInventsOne()
    {
        using TemporaryDirectory temporary = new();
        ScheduledRunPersistence persistence = new(Path.Combine(temporary.Path, "scheduled-runs"));
        ScheduledRunHandle run = await persistence.CreateAsync();
        StubTaskScheduler scheduler = new(ActiveSnapshot(lastResult: 0) with
        {
            IsRunning = true,
            LastRunTime = null,
        });
        SchedulingViewModel withFallback = CreateViewModel(
            temporary,
            scheduler,
            persistence: persistence);

        await withFallback.InitializeAsync(isReadOnly: false, CancellationToken.None);

        Assert.AreEqual(UiStrings.SchedulingStateActive, withFallback.StateText);
        StringAssert.Contains(withFallback.ExecutionText, "environ");
        StringAssert.Contains(
            withFallback.ExecutionText,
            run.StartedAt.ToLocalTime().Year.ToString(CultureInfo.InvariantCulture));

        using TemporaryDirectory emptyTemporary = new();
        SchedulingViewModel withoutFallback = CreateViewModel(
            emptyTemporary,
            scheduler,
            persistence: new ScheduledRunPersistence(Path.Combine(emptyTemporary.Path, "scheduled-runs")));
        await withoutFallback.InitializeAsync(isReadOnly: false, CancellationToken.None);
        Assert.AreEqual(UiStrings.SchedulingStateActive, withoutFallback.StateText);
        Assert.AreEqual(UiStrings.SchedulingRunning, withoutFallback.ExecutionText);
    }

    [TestMethod]
    public async Task NeverRunSentinelSuppressesTheSentinelDate()
    {
        using TemporaryDirectory temporary = new();
        ScheduledTaskSnapshot neverRun = ActiveSnapshot(lastResult: 0x00041303) with
        {
            LastRunTime = null,
        };
        SchedulingViewModel viewModel = CreateViewModel(temporary, new StubTaskScheduler(neverRun));

        await viewModel.InitializeAsync(isReadOnly: false, CancellationToken.None);

        Assert.AreEqual(UiStrings.SchedulingNeverRun, viewModel.LastRunText);
        Assert.AreEqual(UiStrings.SchedulingNeverRun, viewModel.LastResultText);
    }

    [TestMethod]
    public async Task AbsentAndDisabledStatesRemainHonest()
    {
        using TemporaryDirectory absentTemporary = new();
        SchedulingViewModel absent = CreateViewModel(
            absentTemporary,
            new StubTaskScheduler(ScheduledTaskSnapshot.Absent));
        await absent.InitializeAsync(isReadOnly: false, CancellationToken.None);
        Assert.AreEqual(UiStrings.SchedulingStateAbsent, absent.StateText);

        using TemporaryDirectory disabledTemporary = new();
        ScheduledTaskSnapshot disabledSnapshot = ActiveSnapshot(lastResult: 0) with
        {
            DisplayState = ScheduledTaskDisplayState.Disabled,
            IsEnabled = false,
        };
        SchedulingViewModel disabled = CreateViewModel(
            disabledTemporary,
            new StubTaskScheduler(disabledSnapshot));
        await disabled.InitializeAsync(isReadOnly: false, CancellationToken.None);
        Assert.AreEqual(UiStrings.SchedulingStateDisabled, disabled.StateText);
        StringAssert.Contains(disabled.NextRunText, "désactivée");
    }

    [TestMethod]
    public async Task ReadFailureDisablesMutationsAndShowsTheMappedError()
    {
        using TemporaryDirectory temporary = new();
        StubTaskScheduler scheduler = new(ScheduledTaskSnapshot.Absent)
        {
            ReadException = new TaskSchedulerServiceException(
                "read",
                new TestSchedulerException(unchecked((int)0x80041315))),
        };
        SchedulingViewModel viewModel = CreateViewModel(temporary, scheduler);

        await viewModel.InitializeAsync(isReadOnly: false, CancellationToken.None);

        Assert.AreEqual(UiStrings.SchedulingStateReadError, viewModel.StateText);
        StringAssert.Contains(viewModel.OperationMessage, "arrêté");
        Assert.IsFalse(viewModel.CanCreateOrUpdate);
        Assert.IsFalse(viewModel.CanDelete);
    }

    [TestMethod]
    public async Task ConfirmedDeletionCallsTheExactBoundaryOnceAndRefreshesToAbsent()
    {
        using TemporaryDirectory temporary = new();
        StubTaskScheduler scheduler = new(ActiveSnapshot(lastResult: 0));
        StubConfirmation confirmation = new() { Result = true };
        SchedulingViewModel viewModel = CreateViewModel(
            temporary,
            scheduler,
            confirmation: confirmation);
        await viewModel.InitializeAsync(isReadOnly: false, CancellationToken.None);

        viewModel.DeleteCommand.Execute(null);
        await WaitUntilAsync(() => scheduler.DeleteCallCount == 1 && !viewModel.IsBusy);

        Assert.AreEqual(1, confirmation.CallCount);
        Assert.AreEqual(UiStrings.SchedulingStateAbsent, viewModel.StateText);
        Assert.IsFalse(viewModel.CanDelete);
    }

    [TestMethod]
    [DataRow(unchecked((int)0x80070005), "Accès refusé")]
    [DataRow(unchecked((int)0x80070002), "n'existe plus")]
    [DataRow(unchecked((int)0x80041315), "arrêté")]
    [DataRow(unchecked((int)0x80041320), "connecté")]
    [DataRow(unchecked((int)0x80041322), "indisponible")]
    public void HResultMappingProducesActionableFrenchMessages(int hResult, string expectedText)
    {
        TestSchedulerException exception = new(hResult);

        string result = TaskSchedulerErrorFormatter.Format(exception);

        StringAssert.Contains(result, expectedText);
    }

    private static SchedulingViewModel CreateViewModel(
        TemporaryDirectory temporary,
        StubTaskScheduler scheduler,
        ScheduledTaskContract? contract = null,
        IReadOnlyList<string>? blockedNames = null,
        ScheduledRunPersistence? persistence = null,
        StubConfirmation? confirmation = null)
    {
        ScheduledTaskContract effectiveContract = contract ?? CreateContract(temporary);
        return new SchedulingViewModel(
            scheduler,
            confirmation ?? new StubConfirmation(),
            persistence ?? new ScheduledRunPersistence(Path.Combine(temporary.Path, "scheduled-runs")),
            contract is null && scheduler.UseNullContract ? null : effectiveContract,
            blockedNames ?? []);
    }

    private static ScheduledTaskContract CreateContract(TemporaryDirectory temporary)
    {
        string companionPath = Path.Combine(temporary.Path, "CortexCompanion.exe");
        File.WriteAllText(companionPath, "sentinel");
        return new ScheduledTaskContract(
            companionPath,
            temporary.CreateFakeCli(),
            Path.Combine(temporary.Path, "ingestion.toml"),
            Path.Combine(temporary.Path, "confluence.toml"),
            Path.Combine(temporary.Path, "scheduled-runs"),
            AppConstants.IngestionSourceKind,
            @"NYX\User");
    }

    private static ScheduledTaskSnapshot ActiveSnapshot(
        int lastResult,
        ScheduledTaskDisplayState displayState = ScheduledTaskDisplayState.Active) => new(
        displayState,
        true,
        true,
        true,
        false,
        DateTimeOffset.UtcNow.AddHours(1),
        DateTimeOffset.UtcNow.AddHours(-1),
        lastResult,
        SchedulingPreset.Daily,
        new TimeOnly(2, 0));

    private static ScheduledTaskSnapshot CollisionSnapshot() => new(
        ScheduledTaskDisplayState.Collision,
        true,
        false,
        true,
        false,
        null,
        null,
        null,
        null,
        null);

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(2);
        while (!condition())
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                Assert.Fail("The asynchronous scheduling condition was not reached.");
            }

            await Task.Delay(10);
        }
    }

    private sealed class StubTaskScheduler(ScheduledTaskSnapshot snapshot) : ITaskSchedulerService
    {
        private ScheduledTaskSnapshot _snapshot = snapshot;

        public bool UseNullContract { get; set; }
        public ScheduledTaskSnapshot? SnapshotAfterMutation { get; init; }
        public Exception? ReadException { get; init; }
        public Exception? CreateOrUpdateException { get; init; }
        public Exception? DeleteException { get; init; }
        public List<ScheduledTaskContract?> ReadContracts { get; } = [];
        public int CreateOrUpdateAttemptCount { get; private set; }
        public int DeleteAttemptCount { get; private set; }
        public int CreateOrUpdateCallCount { get; private set; }
        public int DeleteCallCount { get; private set; }
        public ScheduledTaskRegistration? LastRegistration { get; private set; }

        public Task<ScheduledTaskSnapshot> ReadAsync(
            ScheduledTaskContract? expectedContract,
            CancellationToken cancellationToken)
        {
            ReadContracts.Add(expectedContract);
            if (ReadException is not null)
            {
                return Task.FromException<ScheduledTaskSnapshot>(ReadException);
            }

            return Task.FromResult(_snapshot);
        }

        public Task CreateOrUpdateAsync(
            ScheduledTaskRegistration registration,
            CancellationToken cancellationToken)
        {
            CreateOrUpdateAttemptCount++;
            if (CreateOrUpdateException is not null)
            {
                return Task.FromException(CreateOrUpdateException);
            }

            CreateOrUpdateCallCount++;
            LastRegistration = registration;
            _snapshot = SnapshotAfterMutation ?? _snapshot;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(CancellationToken cancellationToken)
        {
            DeleteAttemptCount++;
            if (DeleteException is not null)
            {
                return Task.FromException(DeleteException);
            }

            DeleteCallCount++;
            _snapshot = ScheduledTaskSnapshot.Absent;
            return Task.CompletedTask;
        }
    }

    private sealed class StubConfirmation : ISchedulingConfirmationService
    {
        public bool Result { get; init; } = true;
        public int CallCount { get; private set; }

        public bool ConfirmDelete()
        {
            CallCount++;
            return Result;
        }
    }

    private sealed class TestSchedulerException : Exception
    {
        public TestSchedulerException(int hResult)
            : base("sentinel")
        {
            HResult = hResult;
        }
    }
}
