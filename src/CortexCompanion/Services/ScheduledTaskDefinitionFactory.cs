// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using CortexCompanion.Constants;
using CortexCompanion.Localization;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Builds and compares the complete immutable scheduled-task definition contract.</summary>
public static class ScheduledTaskDefinitionFactory
{
    private const string StartBoundaryFormat = "yyyy-MM-dd'T'HH:mm:ss";

    /// <summary>Builds one complete registration from the selected version-zero preset.</summary>
    public static ScheduledTaskRegistration Create(
        ScheduledTaskContract contract,
        SchedulingPreset preset,
        TimeOnly startTime,
        DateOnly? localDate = null)
    {
        ArgumentNullException.ThrowIfNull(contract);
        DateOnly date = localDate ?? DateOnly.FromDateTime(DateTime.Today);
        DateTime boundary = date.ToDateTime(startTime);
        IReadOnlyList<string> workerArguments = ScheduledWorkerArguments.BuildWorkerArguments(
            contract.RunsRoot,
            contract.CliPath,
            contract.IngestionConfigPath,
            contract.ConfluenceConfigPath,
            contract.SourceKind);
        return new ScheduledTaskRegistration(
            contract,
            UiStrings.SchedulingTaskContractDescription,
            new ScheduledTaskActionSpec(
                AppConstants.ScheduledTaskActionExec,
                contract.CompanionPath,
                WindowsCommandLine.Join(workerArguments),
                Path.GetDirectoryName(contract.CompanionPath) ?? string.Empty),
            new ScheduledTaskPrincipalSpec(
                contract.UserId,
                AppConstants.ScheduledTaskLogonInteractiveToken,
                AppConstants.ScheduledTaskRunLevelLua),
            new ScheduledTaskSettingsSpec(
                true,
                AppConstants.ScheduledTaskInstancesIgnoreNew,
                false,
                false,
                false,
                false,
                false,
                AppConstants.ScheduledTaskExecutionTimeLimit,
                true),
            preset,
            startTime,
            boundary.ToString(StartBoundaryFormat, CultureInfo.InvariantCulture),
            preset == SchedulingPreset.Hourly ? AppConstants.ScheduledTaskHourlyInterval : null,
            preset == SchedulingPreset.Hourly ? AppConstants.ScheduledTaskHourlyDuration : null);
    }

    /// <summary>Checks every mutable definition field while excluding ownership from conformity.</summary>
    public static bool IsConforming(
        ScheduledTaskObservedDefinition observed,
        ScheduledTaskContract expected)
    {
        ArgumentNullException.ThrowIfNull(observed);
        ArgumentNullException.ThrowIfNull(expected);
        IReadOnlyList<string> workerArguments = ScheduledWorkerArguments.BuildWorkerArguments(
            expected.RunsRoot,
            expected.CliPath,
            expected.IngestionConfigPath,
            expected.ConfluenceConfigPath,
            expected.SourceKind);
        bool validRepetition = string.IsNullOrEmpty(observed.RepetitionInterval) &&
                               string.IsNullOrEmpty(observed.RepetitionDuration) ||
                               string.Equals(
                                   observed.RepetitionInterval,
                                   AppConstants.ScheduledTaskHourlyInterval,
                                   StringComparison.Ordinal) &&
                               string.Equals(
                                   observed.RepetitionDuration,
                                   AppConstants.ScheduledTaskHourlyDuration,
                                   StringComparison.Ordinal);
        return string.Equals(
                   observed.Description,
                   UiStrings.SchedulingTaskContractDescription,
                   StringComparison.Ordinal) &&
               PathEquals(observed.ActionPath, expected.CompanionPath) &&
               string.Equals(observed.ActionArguments, WindowsCommandLine.Join(workerArguments), StringComparison.Ordinal) &&
               PathEquals(observed.WorkingDirectory, Path.GetDirectoryName(expected.CompanionPath) ?? string.Empty) &&
               UserIdsMatch(observed.UserId, expected.UserId) &&
               observed.LogonType == AppConstants.ScheduledTaskLogonInteractiveToken &&
               observed.RunLevel == AppConstants.ScheduledTaskRunLevelLua &&
               observed.StartWhenAvailable &&
               observed.MultipleInstances == AppConstants.ScheduledTaskInstancesIgnoreNew &&
               !observed.DisallowStartIfOnBatteries &&
               !observed.StopIfGoingOnBatteries &&
               !observed.RunOnlyIfNetworkAvailable &&
               !observed.RunOnlyIfIdle &&
               !observed.StopOnIdleEnd &&
               string.Equals(
                   observed.ExecutionTimeLimit,
                   AppConstants.ScheduledTaskExecutionTimeLimit,
                   StringComparison.Ordinal) &&
               observed.ActionCount == 1 &&
               observed.ActionType == AppConstants.ScheduledTaskActionExec &&
               observed.TriggerCount == 1 &&
               observed.TriggerType == AppConstants.ScheduledTaskTriggerDaily &&
               observed.DaysInterval == 1 &&
               TryParseStartBoundary(observed.StartBoundary, out _) &&
               validRepetition &&
               !observed.StopAtDurationEnd;
    }

    /// <summary>Extracts the supported preset and local start time from a conforming trigger shape.</summary>
    public static bool TryReadPreset(
        ScheduledTaskObservedDefinition observed,
        out SchedulingPreset preset,
        out TimeOnly startTime)
    {
        preset = SchedulingPreset.Daily;
        startTime = default;
        if (!TryParseStartBoundary(observed.StartBoundary, out DateTime boundary))
        {
            return false;
        }

        if (string.IsNullOrEmpty(observed.RepetitionInterval) &&
            string.IsNullOrEmpty(observed.RepetitionDuration))
        {
            preset = SchedulingPreset.Daily;
        }
        else if (string.Equals(
                     observed.RepetitionInterval,
                     AppConstants.ScheduledTaskHourlyInterval,
                     StringComparison.Ordinal) &&
                 string.Equals(
                     observed.RepetitionDuration,
                     AppConstants.ScheduledTaskHourlyDuration,
                     StringComparison.Ordinal))
        {
            preset = SchedulingPreset.Hourly;
        }
        else
        {
            return false;
        }

        startTime = TimeOnly.FromDateTime(boundary);
        return true;
    }

    private static bool TryParseStartBoundary(string value, out DateTime result) =>
        DateTime.TryParseExact(
            value,
            StartBoundaryFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result);

    private static bool PathEquals(string first, string second)
    {
        try
        {
            return string.Equals(
                Path.GetFullPath(first),
                Path.GetFullPath(second),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            return false;
        }
    }

    private static bool UserIdsMatch(string first, string second)
    {
        if (string.Equals(first, second, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string firstLeaf = first.Split('\\').LastOrDefault() ?? first;
        string secondLeaf = second.Split('\\').LastOrDefault() ?? second;
        return string.Equals(firstLeaf, secondLeaf, StringComparison.OrdinalIgnoreCase);
    }
}
