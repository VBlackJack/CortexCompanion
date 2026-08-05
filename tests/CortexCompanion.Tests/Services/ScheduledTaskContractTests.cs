// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;
using CortexCompanion.Localization;
using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class ScheduledTaskContractTests
{
    [TestMethod]
    public void HourlyPresetProducesTwentyFourDistinctOccurrencesWithoutDayBoundaryDuplication()
    {
        using TemporaryDirectory temporary = new();
        ScheduledTaskRegistration registration = ScheduledTaskDefinitionFactory.Create(
            CreateContract(temporary),
            SchedulingPreset.Hourly,
            new TimeOnly(6, 30),
            new DateOnly(2026, 8, 5));
        DateTime start = new(2026, 8, 5, 6, 30, 0, DateTimeKind.Local);
        DateTime[] occurrences = Enumerable.Range(0, 24).Select(index => start.AddHours(index)).ToArray();

        Assert.AreEqual("PT1H", registration.RepetitionInterval);
        Assert.AreEqual("PT23H", registration.RepetitionDuration);
        Assert.AreEqual(24, occurrences.Distinct().Count());
        Assert.AreNotEqual(start.AddDays(1), occurrences[^1]);
        Assert.AreEqual(start.AddHours(23), occurrences[^1]);
    }

    [TestMethod]
    public void DailyPresetHasOneDailyTriggerAndNoRepetition()
    {
        using TemporaryDirectory temporary = new();
        ScheduledTaskRegistration registration = ScheduledTaskDefinitionFactory.Create(
            CreateContract(temporary),
            SchedulingPreset.Daily,
            new TimeOnly(21, 45),
            new DateOnly(2026, 8, 5));

        Assert.AreEqual("2026-08-05T21:45:00", registration.StartBoundary);
        Assert.IsNull(registration.RepetitionInterval);
        Assert.IsNull(registration.RepetitionDuration);
    }

    [TestMethod]
    public void RegistrationFreezesAbsoluteActionPrincipalAndAllExplicitSettings()
    {
        using TemporaryDirectory temporary = new();
        ScheduledTaskContract contract = CreateContract(temporary);
        ScheduledTaskRegistration registration = ScheduledTaskDefinitionFactory.Create(
            contract,
            SchedulingPreset.Daily,
            new TimeOnly(2, 0),
            new DateOnly(2026, 8, 5));

        Assert.AreEqual(Path.GetFullPath(contract.CompanionPath), registration.Action.Path);
        Assert.AreEqual(Path.GetDirectoryName(contract.CompanionPath), registration.Action.WorkingDirectory);
        Assert.AreEqual(0, registration.Action.Type);
        Assert.AreEqual(contract.UserId, registration.Principal.UserId);
        Assert.AreEqual(3, registration.Principal.LogonType);
        Assert.AreEqual(0, registration.Principal.RunLevel);
        Assert.IsTrue(registration.Settings.StartWhenAvailable);
        Assert.AreEqual(2, registration.Settings.MultipleInstances);
        Assert.IsFalse(registration.Settings.DisallowStartIfOnBatteries);
        Assert.IsFalse(registration.Settings.StopIfGoingOnBatteries);
        Assert.IsFalse(registration.Settings.RunOnlyIfNetworkAvailable);
        Assert.IsFalse(registration.Settings.RunOnlyIfIdle);
        Assert.IsFalse(registration.Settings.StopOnIdleEnd);
        Assert.AreEqual("PT0S", registration.Settings.ExecutionTimeLimit);
        Assert.IsTrue(registration.Settings.Enabled);
        StringAssert.Contains(registration.Action.Arguments, "--scheduled-worker");
        StringAssert.Contains(registration.Action.Arguments, "--ingestion-config-path");
        StringAssert.Contains(registration.Action.Arguments, "--confluence-config-path");
    }

    [TestMethod]
    public void CompleteDefinitionConformsAndActionPathDivergenceDoesNotChangeOwnership()
    {
        using TemporaryDirectory temporary = new();
        ScheduledTaskContract contract = CreateContract(temporary);
        ScheduledTaskRegistration registration = ScheduledTaskDefinitionFactory.Create(
            contract,
            SchedulingPreset.Daily,
            new TimeOnly(2, 0),
            new DateOnly(2026, 8, 5));
        ScheduledTaskObservedDefinition observed = CreateObserved(registration);

        Assert.IsTrue(ScheduledTaskDefinitionFactory.IsConforming(observed, contract));
        Assert.IsFalse(ScheduledTaskDefinitionFactory.IsConforming(
            observed with { ActionPath = Path.Combine(temporary.Path, "moved", "CortexCompanion.exe") },
            contract));
        Assert.IsTrue(ScheduledTaskOwnershipPolicy.IsOwned(AppConstants.ScheduledTaskOwnershipToken));
    }

    [TestMethod]
    public void DivergentIngestionPathInThePersistedActionFailsConformity()
    {
        using TemporaryDirectory temporary = new();
        ScheduledTaskContract contract = CreateContract(temporary);
        ScheduledTaskRegistration registration = ScheduledTaskDefinitionFactory.Create(
            contract,
            SchedulingPreset.Daily,
            new TimeOnly(2, 0),
            new DateOnly(2026, 8, 5));
        ScheduledTaskObservedDefinition observed = CreateObserved(registration);
        string divergentArguments = registration.Action.Arguments.Replace(
            contract.IngestionConfigPath,
            Path.Combine(temporary.Path, "other-ingestion.toml"),
            StringComparison.Ordinal);

        Assert.AreNotEqual(registration.Action.Arguments, divergentArguments);
        Assert.IsFalse(ScheduledTaskDefinitionFactory.IsConforming(
            observed with { ActionArguments = divergentArguments },
            contract));
    }

    [TestMethod]
    public void OwnershipIsStrictAndForeignTokensFailClosed()
    {
        Assert.IsFalse(ScheduledTaskOwnershipPolicy.IsOwned(null));
        Assert.IsFalse(ScheduledTaskOwnershipPolicy.IsOwned("CortexCompanion"));
        Assert.ThrowsExactly<TaskSchedulerCollisionException>(() =>
            ScheduledTaskOwnershipPolicy.EnsureOwned("foreign-token"));
    }

    [TestMethod]
    public void WindowsArgumentsQuoteEveryPathWithSpacesWithoutAShell()
    {
        string result = WindowsCommandLine.Join(
            ["--scheduled-worker", "--cli-path", @"C:\Program Files\Cortex\cortex.exe"]);

        Assert.AreEqual(
            "--scheduled-worker --cli-path \"C:\\Program Files\\Cortex\\cortex.exe\"",
            result);
    }

    [TestMethod]
    public void ReapplyingTheSamePresetProducesTheSameIdempotentDefinition()
    {
        using TemporaryDirectory temporary = new();
        ScheduledTaskContract contract = CreateContract(temporary);

        ScheduledTaskRegistration first = ScheduledTaskDefinitionFactory.Create(
            contract,
            SchedulingPreset.Hourly,
            new TimeOnly(7, 15),
            new DateOnly(2026, 8, 5));
        ScheduledTaskRegistration second = ScheduledTaskDefinitionFactory.Create(
            contract,
            SchedulingPreset.Hourly,
            new TimeOnly(7, 15),
            new DateOnly(2026, 8, 5));

        Assert.AreEqual(first, second);
    }

    private static ScheduledTaskContract CreateContract(TemporaryDirectory temporary)
    {
        string companionPath = Path.Combine(temporary.Path, "CortexCompanion.exe");
        string cliPath = temporary.CreateFakeCli();
        File.WriteAllText(companionPath, "sentinel");
        return new ScheduledTaskContract(
            companionPath,
            cliPath,
            Path.Combine(temporary.Path, "ingestion.toml"),
            Path.Combine(temporary.Path, "confluence.toml"),
            Path.Combine(temporary.Path, "scheduled-runs"),
            AppConstants.IngestionSourceKind,
            @"NYX\User");
    }

    private static ScheduledTaskObservedDefinition CreateObserved(ScheduledTaskRegistration registration) => new(
        UiStrings.SchedulingTaskContractDescription,
        registration.Contract.CompanionPath,
        registration.Action.Arguments,
        Path.GetDirectoryName(registration.Contract.CompanionPath)!,
        "User",
        3,
        0,
        true,
        2,
        false,
        false,
        false,
        false,
        false,
        AppConstants.ScheduledTaskExecutionTimeLimit,
        1,
        0,
        1,
        2,
        1,
        registration.StartBoundary,
        registration.RepetitionInterval ?? string.Empty,
        registration.RepetitionDuration ?? string.Empty,
        false);
}
