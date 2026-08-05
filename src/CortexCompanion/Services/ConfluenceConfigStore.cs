// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using CortexCompanion.Interfaces;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Replicates Cortex A2 exact-byte CAS, backup, validation, and Windows byte-lock semantics.</summary>
public sealed partial class ConfluenceConfigStore : IConfluenceConfigStore
{
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(50);
    private readonly string _path;

    /// <summary>Initializes the store for the absolute session configuration path.</summary>
    public ConfluenceConfigStore(string path)
    {
        _path = Path.GetFullPath(path ?? throw new ArgumentNullException(nameof(path)));
    }

    /// <summary>Gets the common Python/C# mutation lock path.</summary>
    public string LockPath => _path + ".mutation.lock";

    /// <summary>Gets the exact previous-byte backup path.</summary>
    public string BackupPath => _path + ".bak";

    internal static async Task<IAsyncDisposable> AcquireMutationLockForInteropAsync(
        string configurationPath,
        CancellationToken cancellationToken) =>
        await ConfluenceMutationLock.AcquireAsync(
            Path.GetFullPath(configurationPath) + ".mutation.lock",
            LockTimeout,
            RetryDelay,
            cancellationToken);

    /// <inheritdoc />
    public async Task<ConfluenceConfigSnapshot> ReadAsync(CancellationToken cancellationToken)
    {
        byte[] content = await File.ReadAllBytesAsync(_path, cancellationToken);
        return Snapshot(content, _path);
    }

    /// <inheritdoc />
    public async Task<ConfluenceConfigSnapshot> WriteAsync(
        ConfluenceConfiguration configuration,
        string? expectedHash,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (expectedHash is not null && !Sha256Pattern().IsMatch(expectedHash))
        {
            throw new ArgumentException("expectedHash must be a lowercase SHA-256 hexadecimal value.", nameof(expectedHash));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_path)
            ?? throw new InvalidOperationException("The configuration path has no parent directory."));
        await using ConfluenceMutationLock mutationLock = await ConfluenceMutationLock.AcquireAsync(
            LockPath,
            LockTimeout,
            RetryDelay,
            cancellationToken);

        byte[]? current;
        try
        {
            current = await File.ReadAllBytesAsync(_path, cancellationToken);
        }
        catch (FileNotFoundException)
        {
            current = null;
        }

        if (expectedHash is null && current is not null)
        {
            throw new ConfluenceConfigConflictException(
                "Confluence configuration appeared after the caller snapshot.");
        }

        if (expectedHash is not null && current is null)
        {
            throw new ConfluenceConfigConflictException(
                "Confluence configuration disappeared after the caller snapshot.");
        }

        if (expectedHash is not null && current is not null &&
            !string.Equals(Hash(current), expectedHash, StringComparison.Ordinal))
        {
            throw new ConfluenceConfigConflictException(
                "Confluence configuration changed after the caller snapshot.");
        }

        byte[] rendered = ConfluenceConfigRenderer.Render(configuration);
        string? configTemporary = null;
        string? backupTemporary = null;
        try
        {
            configTemporary = await WriteTemporaryAsync(_path, rendered, cancellationToken);
            byte[] temporaryBytes = await File.ReadAllBytesAsync(configTemporary, cancellationToken);
            ConfluenceConfigSnapshot validated = Snapshot(temporaryBytes, configTemporary);
            if (!validated.Configuration.SemanticallyEquals(configuration))
            {
                throw new ConfluenceConfigMutationException(
                    "Canonical Confluence TOML did not round-trip to the requested settings.");
            }

            if (current is not null)
            {
                backupTemporary = await WriteTemporaryAsync(BackupPath, current, cancellationToken);
                File.Move(backupTemporary, BackupPath, overwrite: true);
                backupTemporary = null;
            }
            File.Move(configTemporary, _path, overwrite: true);
            configTemporary = null;
            return validated;
        }
        finally
        {
            DeleteIfPresent(configTemporary);
            DeleteIfPresent(backupTemporary);
        }
    }

    private static ConfluenceConfigSnapshot Snapshot(byte[] content, string sourcePath) =>
        new(content, Hash(content), ConfluenceConfigParser.Parse(content, sourcePath));

    private static string Hash(byte[] content) => Convert.ToHexStringLower(SHA256.HashData(content));

    private static async Task<string> WriteTemporaryAsync(
        string destination,
        byte[] content,
        CancellationToken cancellationToken)
    {
        string directory = Path.GetDirectoryName(destination)
            ?? throw new InvalidOperationException("The destination has no parent directory.");
        string temporary = Path.Combine(
            directory,
            $".{Path.GetFileName(destination)}.{Path.GetRandomFileName()}.tmp");
        await using FileStream stream = new(
            temporary,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            4096,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
        await stream.WriteAsync(content, cancellationToken);
        await stream.FlushAsync(cancellationToken);
        stream.Flush(flushToDisk: true);
        return temporary;
    }

    private static void DeleteIfPresent(string? path)
    {
        if (path is null)
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A failed best-effort cleanup must not mask the primary mutation result.
        }
    }

    [GeneratedRegex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Pattern();

    private sealed class ConfluenceMutationLock : IAsyncDisposable
    {
        private readonly FileStream _stream;
        private readonly string _path;

        private ConfluenceMutationLock(FileStream stream, string path)
        {
            _stream = stream;
            _path = path;
        }

        public static async Task<ConfluenceMutationLock> AcquireAsync(
            string path,
            TimeSpan timeout,
            TimeSpan retryDelay,
            CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RejectReparsePoint(path);
                FileStream? stream = null;
                try
                {
                    stream = new FileStream(
                        path,
                        FileMode.OpenOrCreate,
                        FileAccess.ReadWrite,
                        FileShare.ReadWrite);
                    RejectReparsePoint(path);
                    stream.Lock(0, 1);
                    return new ConfluenceMutationLock(stream, path);
                }
                catch (IOException) when (stopwatch.Elapsed < timeout)
                {
                    stream?.Dispose();
                    await Task.Delay(retryDelay, cancellationToken);
                }
                catch (IOException exception)
                {
                    stream?.Dispose();
                    throw new ConfluenceConfigLockedException(
                        "Another process is mutating the Confluence configuration.",
                        exception);
                }
            }
        }

        public ValueTask DisposeAsync()
        {
            try
            {
                _stream.Unlock(0, 1);
            }
            finally
            {
                _stream.Dispose();
                try
                {
                    File.Delete(_path);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    // Python filelock also treats lock-file deletion as best effort.
                }
            }

            return ValueTask.CompletedTask;
        }

        private static void RejectReparsePoint(string path)
        {
            if (File.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
            {
                throw new ConfluenceConfigLockedException(
                    "The Confluence mutation lock path must not be a reparse point.");
            }
        }
    }
}

/// <summary>Reports an exact-byte CAS conflict requiring a fresh read and a new user presentation.</summary>
public sealed class ConfluenceConfigConflictException : Exception
{
    /// <summary>Initializes a conflict error.</summary>
    public ConfluenceConfigConflictException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>Reports an owned mutation failure after the CAS check.</summary>
public class ConfluenceConfigMutationException : Exception
{
    /// <summary>Initializes a mutation error.</summary>
    public ConfluenceConfigMutationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>Reports that the shared OS mutation lock cannot be acquired safely.</summary>
public sealed class ConfluenceConfigLockedException : ConfluenceConfigMutationException
{
    /// <summary>Initializes a lock error.</summary>
    public ConfluenceConfigLockedException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
