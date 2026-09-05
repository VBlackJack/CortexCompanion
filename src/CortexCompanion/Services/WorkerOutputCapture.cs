// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text;
using CortexCompanion.Logging;

namespace CortexCompanion.Services;

/// <summary>Keeps draining the child pipe if its durable log becomes unavailable.</summary>
internal static class WorkerOutputCapture
{
    internal const string FailureKind = "OutputPersistenceFailed";
    private const int BufferLength = 4_096;

    internal static Task<bool> CopyAsync(StreamReader source, string destinationPath) =>
        CopyAsync(source, () => new StreamWriter(new FileStream(
            destinationPath, FileMode.Create, FileAccess.Write,
            FileShare.ReadWrite | FileShare.Delete, BufferLength,
            FileOptions.Asynchronous | FileOptions.WriteThrough), new UTF8Encoding(false))
        {
            AutoFlush = true,
        });

    internal static async Task<bool> CopyAsync(TextReader source, Func<TextWriter> openDestination)
    {
        char[] buffer = new char[BufferLength];
        try
        {
            using TextWriter destination = openDestination();
            int count;
            while ((count = await source.ReadAsync(buffer.AsMemory())) != 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, count));
            }
            await destination.FlushAsync();
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            FileLogger.Error("Worker output persistence failed; draining the remaining pipe", exception);
        }

        // A failed log must never leave the child waiting for room in its pipe.
        // Preserve its lifetime and report the capture failure after it completes.
        while (await source.ReadAsync(buffer.AsMemory()) != 0)
        {
        }
        return false;
    }
}
