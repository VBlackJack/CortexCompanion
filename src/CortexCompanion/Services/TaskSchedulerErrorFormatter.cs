// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Localization;

namespace CortexCompanion.Services;

/// <summary>Maps Task Scheduler HRESULT values to actionable French UI messages.</summary>
public static class TaskSchedulerErrorFormatter
{
    private const int ErrorFileNotFound = unchecked((int)0x80070002);
    private const int ErrorAccessDenied = unchecked((int)0x80070005);
    private const int SchedulerServiceNotRunning = unchecked((int)0x80041315);
    private const int SchedulerUserNotLoggedOn = unchecked((int)0x80041320);
    private const int SchedulerServiceNotAvailable = unchecked((int)0x80041322);

    /// <summary>Formats one native scheduler failure without relying on its exception subtype.</summary>
    public static string Format(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        int hResult = exception.HResult;
        return hResult switch
        {
            ErrorFileNotFound => UiStrings.SchedulingErrorMissing,
            ErrorAccessDenied => UiStrings.SchedulingErrorAccessDenied,
            SchedulerServiceNotRunning => UiStrings.SchedulingErrorServiceNotRunning,
            SchedulerUserNotLoggedOn => UiStrings.SchedulingErrorUserNotLoggedOn,
            SchedulerServiceNotAvailable => UiStrings.SchedulingErrorServiceUnavailable,
            _ => UiStrings.FormatSchedulingErrorUnexpected(unchecked((uint)hResult)),
        };
    }
}
