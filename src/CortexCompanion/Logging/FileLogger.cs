// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using CortexCompanion.Constants;

namespace CortexCompanion.Logging;

/// <summary>
/// Provides a non-throwing daily rotating file logger for startup and lightweight services.
/// </summary>
public static class FileLogger
{
    private static readonly ConcurrentQueue<string> Queue = new();
    private static readonly Lock SyncRoot = new();
    private static Timer? _flushTimer;
    private static string? _logDirectory;
    private static bool _enabled = true;

    /// <summary>Initializes the logger for the required local application directory.</summary>
    public static void Initialize(string logDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logDirectory);

        try
        {
            Directory.CreateDirectory(logDirectory);
            lock (SyncRoot)
            {
                _logDirectory = logDirectory;
                _flushTimer?.Dispose();
                _flushTimer = new Timer(
                    static _ => Flush(),
                    null,
                    AppConstants.LogFlushInterval,
                    AppConstants.LogFlushInterval);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"FileLogger initialization failed: {exception.GetType().Name}");
        }
    }

    /// <summary>Enables or disables future log messages.</summary>
    public static void SetEnabled(bool enabled) => _enabled = enabled;

    /// <summary>Queues an informational message.</summary>
    public static void Info(string message) => Enqueue("INFO", message);

    /// <summary>Queues a warning message.</summary>
    public static void Warn(string message) => Enqueue("WARN", message);

    /// <summary>Queues an error without writing sensitive process output or settings content.</summary>
    public static void Error(string message, Exception? exception = null)
    {
        string suffix = exception is null ? string.Empty : $" ({FormatException(exception)})";
        Enqueue("ERROR", message + suffix);
    }

    /// <summary>Flushes queued messages to the current daily log file.</summary>
    public static void Flush()
    {
        if (!_enabled || Queue.IsEmpty)
        {
            return;
        }

        try
        {
            lock (SyncRoot)
            {
                if (string.IsNullOrWhiteSpace(_logDirectory))
                {
                    return;
                }

                string logPath = Path.Combine(
                    _logDirectory,
                    $"{AppConstants.AppName}_{DateTime.Now:yyyyMMdd}.log");
                StringBuilder batch = new();
                while (Queue.TryDequeue(out string? line))
                {
                    batch.AppendLine(line);
                }

                File.AppendAllText(logPath, batch.ToString(), Encoding.UTF8);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"FileLogger flush failed: {exception.GetType().Name}");
        }
    }

    private static void Enqueue(string level, string message)
    {
        if (!_enabled || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        string line = string.Create(
            CultureInfo.InvariantCulture,
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");
        Queue.Enqueue(line);
        Debug.WriteLine(line);
    }

    private static string FormatException(Exception exception)
    {
        string diagnostic = exception.ToString()
            .Replace("\r\n", " | ", StringComparison.Ordinal)
            .Replace('\r', ' ')
            .Replace('\n', ' ');
        return diagnostic.Length <= AppConstants.MaxExceptionDiagnosticCharacters
            ? diagnostic
            : diagnostic[..AppConstants.MaxExceptionDiagnosticCharacters];
    }
}

