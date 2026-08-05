// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Runtime.InteropServices;
using CortexCompanion.Constants;
using CortexCompanion.Interfaces;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Confines all dynamic Task Scheduler COM automation to the exact Companion task.</summary>
public sealed class TaskSchedulerComAdapter : ITaskSchedulerService
{
    private const int TaskCreateOrUpdate = 6;
    private const int TaskActionExec = 0;
    private const int TaskTriggerDaily = 2;
    private const int TaskStateDisabled = 1;
    private const int TaskStateRunning = 4;
    private const int ErrorFileNotFound = unchecked((int)0x80070002);

    /// <inheritdoc />
    public Task<ScheduledTaskSnapshot> ReadAsync(
        ScheduledTaskContract? expectedContract,
        CancellationToken cancellationToken) =>
        Task.Run(() => ReadCore(expectedContract), cancellationToken);

    /// <inheritdoc />
    public Task CreateOrUpdateAsync(
        ScheduledTaskRegistration registration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(registration);
        return Task.Run(() => CreateOrUpdateCore(registration), cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteAsync(CancellationToken cancellationToken) =>
        Task.Run(DeleteCore, cancellationToken);

    private static ScheduledTaskSnapshot ReadCore(ScheduledTaskContract? expectedContract)
    {
        dynamic? service = null;
        dynamic? folder = null;
        dynamic? registeredTask = null;
        dynamic? definition = null;
        try
        {
            service = Connect();
            try
            {
                folder = service.GetFolder(AppConstants.ScheduledTaskFolderPath);
                registeredTask = folder.GetTask(TaskName);
            }
            catch (Exception exception) when (exception.HResult == ErrorFileNotFound)
            {
                return ScheduledTaskSnapshot.Absent;
            }

            definition = registeredTask.Definition;
            string source = ReadSource(definition);
            bool isOwned = ScheduledTaskOwnershipPolicy.IsOwned(source);
            bool isEnabled = Convert.ToBoolean(registeredTask.Enabled, CultureInfo.InvariantCulture);
            int state = Convert.ToInt32(registeredTask.State, CultureInfo.InvariantCulture);
            DateTimeOffset? nextRunTime = ConvertTaskDate(registeredTask.NextRunTime);
            DateTimeOffset? lastRunTime = ConvertTaskDate(registeredTask.LastRunTime);
            int lastTaskResult = Convert.ToInt32(registeredTask.LastTaskResult, CultureInfo.InvariantCulture);
            if (!isOwned)
            {
                return new ScheduledTaskSnapshot(
                    ScheduledTaskDisplayState.Collision,
                    true,
                    false,
                    isEnabled,
                    state == TaskStateRunning,
                    nextRunTime,
                    lastRunTime,
                    lastTaskResult,
                    null,
                    null);
            }

            ScheduledTaskObservedDefinition observed = ReadObservedDefinition(definition);
            bool conforming = expectedContract is null ||
                              ScheduledTaskDefinitionFactory.IsConforming(observed, expectedContract);
            SchedulingPreset? preset = null;
            TimeOnly? startTime = null;
            if (ScheduledTaskDefinitionFactory.TryReadPreset(
                    observed,
                    out SchedulingPreset observedPreset,
                    out TimeOnly observedStartTime))
            {
                preset = observedPreset;
                startTime = observedStartTime;
            }

            ScheduledTaskDisplayState displayState = !conforming
                ? ScheduledTaskDisplayState.NeedsReconfiguration
                : state == TaskStateDisabled || !isEnabled
                    ? ScheduledTaskDisplayState.Disabled
                    : ScheduledTaskDisplayState.Active;
            return new ScheduledTaskSnapshot(
                displayState,
                true,
                true,
                isEnabled,
                state == TaskStateRunning,
                nextRunTime,
                lastRunTime,
                lastTaskResult,
                preset,
                startTime);
        }
        catch (Exception exception) when (exception is not TaskSchedulerCollisionException)
        {
            throw new TaskSchedulerServiceException("read", exception);
        }
        finally
        {
            Release(definition);
            Release(registeredTask);
            Release(folder);
            Release(service);
        }
    }

    private static void CreateOrUpdateCore(ScheduledTaskRegistration registration)
    {
        dynamic? service = null;
        dynamic? folder = null;
        dynamic? existingTask = null;
        dynamic? existingDefinition = null;
        dynamic? definition = null;
        dynamic? registrationInfo = null;
        dynamic? principal = null;
        dynamic? settings = null;
        dynamic? idleSettings = null;
        dynamic? triggers = null;
        dynamic? trigger = null;
        dynamic? repetition = null;
        dynamic? actions = null;
        dynamic? action = null;
        dynamic? registeredTask = null;
        try
        {
            service = Connect();
            folder = GetOrCreateFolder(service);
            try
            {
                existingTask = folder.GetTask(TaskName);
                existingDefinition = existingTask.Definition;
                EnsureDefinitionOwned(existingDefinition);
            }
            catch (Exception exception) when (exception.HResult == ErrorFileNotFound)
            {
                Release(existingDefinition);
                existingDefinition = null;
                Release(existingTask);
                existingTask = null;
            }

            definition = service.NewTask(0);
            registrationInfo = definition.RegistrationInfo;
            registrationInfo.Source = AppConstants.ScheduledTaskOwnershipToken;
            registrationInfo.Description = registration.Description;

            principal = definition.Principal;
            principal.UserId = registration.Principal.UserId;
            principal.LogonType = registration.Principal.LogonType;
            principal.RunLevel = registration.Principal.RunLevel;

            settings = definition.Settings;
            settings.StartWhenAvailable = registration.Settings.StartWhenAvailable;
            settings.MultipleInstances = registration.Settings.MultipleInstances;
            settings.DisallowStartIfOnBatteries = registration.Settings.DisallowStartIfOnBatteries;
            settings.StopIfGoingOnBatteries = registration.Settings.StopIfGoingOnBatteries;
            settings.RunOnlyIfNetworkAvailable = registration.Settings.RunOnlyIfNetworkAvailable;
            settings.RunOnlyIfIdle = registration.Settings.RunOnlyIfIdle;
            idleSettings = settings.IdleSettings;
            idleSettings.StopOnIdleEnd = registration.Settings.StopOnIdleEnd;
            settings.ExecutionTimeLimit = registration.Settings.ExecutionTimeLimit;
            settings.Enabled = registration.Settings.Enabled;

            triggers = definition.Triggers;
            trigger = triggers.Create(AppConstants.ScheduledTaskTriggerDaily);
            trigger.StartBoundary = registration.StartBoundary;
            trigger.DaysInterval = 1;
            if (registration.Preset == SchedulingPreset.Hourly)
            {
                repetition = trigger.Repetition;
                repetition.Interval = registration.RepetitionInterval;
                repetition.Duration = registration.RepetitionDuration;
                repetition.StopAtDurationEnd = false;
            }

            actions = definition.Actions;
            action = actions.Create(registration.Action.Type);
            action.Path = registration.Action.Path;
            action.Arguments = registration.Action.Arguments;
            action.WorkingDirectory = registration.Action.WorkingDirectory;

            EnsureTargetOwnedIfPresent(folder);
            registeredTask = folder.RegisterTaskDefinition(
                TaskName,
                definition,
                TaskCreateOrUpdate,
                null,
                null,
                registration.Principal.LogonType,
                null);
        }
        catch (TaskSchedulerCollisionException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new TaskSchedulerServiceException("create-or-update", exception);
        }
        finally
        {
            Release(registeredTask);
            Release(action);
            Release(actions);
            Release(repetition);
            Release(trigger);
            Release(triggers);
            Release(idleSettings);
            Release(settings);
            Release(principal);
            Release(registrationInfo);
            Release(definition);
            Release(existingDefinition);
            Release(existingTask);
            Release(folder);
            Release(service);
        }
    }

    private static void DeleteCore()
    {
        dynamic? service = null;
        dynamic? folder = null;
        dynamic? registeredTask = null;
        dynamic? definition = null;
        try
        {
            service = Connect();
            try
            {
                folder = service.GetFolder(AppConstants.ScheduledTaskFolderPath);
                registeredTask = folder.GetTask(TaskName);
            }
            catch (Exception exception) when (exception.HResult == ErrorFileNotFound)
            {
                return;
            }

            definition = registeredTask.Definition;
            EnsureDefinitionOwned(definition);
            folder.DeleteTask(TaskName, 0);
        }
        catch (TaskSchedulerCollisionException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new TaskSchedulerServiceException("delete", exception);
        }
        finally
        {
            Release(definition);
            Release(registeredTask);
            Release(folder);
            Release(service);
        }
    }

    private static dynamic Connect()
    {
        Type serviceType = Type.GetTypeFromProgID("Schedule.Service")
            ?? throw new InvalidOperationException("The Task Scheduler COM service is unavailable.");
        dynamic service = Activator.CreateInstance(serviceType)
            ?? throw new InvalidOperationException("The Task Scheduler COM service could not be created.");
        service.Connect();
        return service;
    }

    private static dynamic GetOrCreateFolder(dynamic service)
    {
        try
        {
            return service.GetFolder(AppConstants.ScheduledTaskFolderPath);
        }
        catch (Exception exception) when (exception.HResult == ErrorFileNotFound)
        {
            dynamic? root = null;
            try
            {
                root = service.GetFolder("\\");
                return root.CreateFolder(AppConstants.ScheduledTaskFolderName);
            }
            finally
            {
                Release(root);
            }
        }
    }

    private static string ReadSource(dynamic definition)
    {
        dynamic? registrationInfo = null;
        try
        {
            registrationInfo = definition.RegistrationInfo;
            return Convert.ToString(registrationInfo.Source, CultureInfo.InvariantCulture) ?? string.Empty;
        }
        finally
        {
            Release(registrationInfo);
        }
    }

    private static void EnsureDefinitionOwned(dynamic definition)
    {
        ScheduledTaskOwnershipPolicy.EnsureOwned(ReadSource(definition));
    }

    private static void EnsureTargetOwnedIfPresent(dynamic folder)
    {
        dynamic? currentTask = null;
        dynamic? currentDefinition = null;
        try
        {
            currentTask = folder.GetTask(TaskName);
            currentDefinition = currentTask.Definition;
            EnsureDefinitionOwned(currentDefinition);
        }
        catch (Exception exception) when (exception.HResult == ErrorFileNotFound)
        {
            return;
        }
        finally
        {
            Release(currentDefinition);
            Release(currentTask);
        }
    }

    private static ScheduledTaskObservedDefinition ReadObservedDefinition(dynamic definition)
    {
        dynamic? registrationInfo = null;
        dynamic? principal = null;
        dynamic? settings = null;
        dynamic? idleSettings = null;
        dynamic? actions = null;
        dynamic? action = null;
        dynamic? triggers = null;
        dynamic? trigger = null;
        dynamic? repetition = null;
        try
        {
            registrationInfo = definition.RegistrationInfo;
            principal = definition.Principal;
            settings = definition.Settings;
            idleSettings = settings.IdleSettings;
            actions = definition.Actions;
            triggers = definition.Triggers;
            int actionCount = Convert.ToInt32(actions.Count, CultureInfo.InvariantCulture);
            int triggerCount = Convert.ToInt32(triggers.Count, CultureInfo.InvariantCulture);
            action = actionCount == 1 ? actions.Item(1) : null;
            trigger = triggerCount == 1 ? triggers.Item(1) : null;
            int actionType = action is null
                ? -1
                : Convert.ToInt32(action.Type, CultureInfo.InvariantCulture);
            int triggerType = trigger is null
                ? -1
                : Convert.ToInt32(trigger.Type, CultureInfo.InvariantCulture);
            repetition = trigger is null ? null : trigger.Repetition;
            return new ScheduledTaskObservedDefinition(
                Convert.ToString(registrationInfo.Description, CultureInfo.InvariantCulture) ?? string.Empty,
                actionType == TaskActionExec
                    ? Convert.ToString(action!.Path, CultureInfo.InvariantCulture) ?? string.Empty
                    : string.Empty,
                actionType == TaskActionExec
                    ? Convert.ToString(action!.Arguments, CultureInfo.InvariantCulture) ?? string.Empty
                    : string.Empty,
                actionType == TaskActionExec
                    ? Convert.ToString(action!.WorkingDirectory, CultureInfo.InvariantCulture) ?? string.Empty
                    : string.Empty,
                Convert.ToString(principal.UserId, CultureInfo.InvariantCulture) ?? string.Empty,
                Convert.ToInt32(principal.LogonType, CultureInfo.InvariantCulture),
                Convert.ToInt32(principal.RunLevel, CultureInfo.InvariantCulture),
                Convert.ToBoolean(settings.StartWhenAvailable, CultureInfo.InvariantCulture),
                Convert.ToInt32(settings.MultipleInstances, CultureInfo.InvariantCulture),
                Convert.ToBoolean(settings.DisallowStartIfOnBatteries, CultureInfo.InvariantCulture),
                Convert.ToBoolean(settings.StopIfGoingOnBatteries, CultureInfo.InvariantCulture),
                Convert.ToBoolean(settings.RunOnlyIfNetworkAvailable, CultureInfo.InvariantCulture),
                Convert.ToBoolean(settings.RunOnlyIfIdle, CultureInfo.InvariantCulture),
                Convert.ToBoolean(idleSettings.StopOnIdleEnd, CultureInfo.InvariantCulture),
                Convert.ToString(settings.ExecutionTimeLimit, CultureInfo.InvariantCulture) ?? string.Empty,
                actionCount,
                actionType,
                triggerCount,
                triggerType,
                triggerType == TaskTriggerDaily
                    ? Convert.ToInt16(trigger!.DaysInterval, CultureInfo.InvariantCulture)
                    : (short)0,
                trigger is null ? string.Empty : Convert.ToString(trigger.StartBoundary, CultureInfo.InvariantCulture) ?? string.Empty,
                repetition is null ? string.Empty : Convert.ToString(repetition.Interval, CultureInfo.InvariantCulture) ?? string.Empty,
                repetition is null ? string.Empty : Convert.ToString(repetition.Duration, CultureInfo.InvariantCulture) ?? string.Empty,
                repetition is not null && Convert.ToBoolean(repetition.StopAtDurationEnd, CultureInfo.InvariantCulture));
        }
        finally
        {
            Release(repetition);
            Release(trigger);
            Release(triggers);
            Release(action);
            Release(actions);
            Release(idleSettings);
            Release(settings);
            Release(principal);
            Release(registrationInfo);
        }
    }

    private static DateTimeOffset? ConvertTaskDate(object value)
    {
        DateTime date = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
        if (date.Year < 2000)
        {
            return null;
        }

        DateTime local = DateTime.SpecifyKind(date, DateTimeKind.Local);
        return new DateTimeOffset(local);
    }

    private static string TaskName => AppConstants.ScheduledTaskNamePrefix + AppConstants.IngestionSourceKind;

    private static void Release(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.FinalReleaseComObject(value);
        }
    }
}
