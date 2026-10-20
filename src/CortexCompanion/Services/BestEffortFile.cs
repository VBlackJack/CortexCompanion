// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Logging;

namespace CortexCompanion.Services;

/// <summary>Removes temporary files without letting a cleanup failure mask the primary result.</summary>
internal static class BestEffortFile
{
    /// <summary>Deletes the file when it exists; a locked or protected file is logged and left behind.</summary>
    /// <remarks>
    /// Callers run this from a finally block. An IOException thrown there would replace the
    /// exception or the result of the operation the block protects, so it is reported at
    /// warning level instead. Only the file name is logged, never the profile path.
    /// </remarks>
    public static void Delete(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            FileLogger.Warn(
                $"Temporary file could not be removed: {Path.GetFileName(path)} ({exception.GetType().Name})");
        }
    }
}
