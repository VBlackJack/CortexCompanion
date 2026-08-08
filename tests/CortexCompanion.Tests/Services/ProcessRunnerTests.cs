// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;
using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class ProcessRunnerTests
{
    [TestMethod]
    public async Task RunAsyncUsesBomlessStrictUtf8ForOutput()
    {
        string probePath = FindLockProbe();
        ProcessRunner runner = new();
        ProcessRequest request = new(
            probePath,
            ["unicode-output"],
            TimeSpan.FromSeconds(10),
            4_096);

        ProcessRunResult result = await runner.RunAsync(request, CancellationToken.None);

        Assert.AreEqual(0, result.ExitCode);
        Assert.IsNull(result.LaunchError);
        Assert.IsFalse(result.OutcomeUnknown);
        Assert.Contains("G:/Équipe/🔒", result.StandardOutput, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task RunAsyncKillsTheRealProcessTreeOnTimeout()
    {
        ProcessRunner runner = new();
        ProcessRequest request = new(
            FindLockProbe(),
            ["spawn-child-tree"],
            TimeSpan.FromMilliseconds(500),
            4_096);

        ProcessRunResult result = await runner.RunAsync(request, CancellationToken.None);

        Assert.IsTrue(result.TimedOut);
        Assert.IsTrue(result.OutcomeUnknown);
        Assert.IsTrue(int.TryParse(result.StandardOutput.Trim(), out int childProcessId));
        await AssertProcessExitedAsync(childProcessId);
    }

    [TestMethod]
    [Timeout(15_000, CooperativeCancellation = true)]
    public async Task RunAsyncTimeoutRemainsBoundedWhenTreeTerminationFails()
    {
        int processId = 0;
        bool terminationRequested = false;
        Stopwatch stopwatch = new();
        ProcessRunner runner = new(process =>
        {
            processId = process.Id;
            terminationRequested = true;
            stopwatch.Restart();
            return Task.FromResult(false);
        });
        ProcessRequest request = new(
            FindLockProbe(),
            ["hold-process"],
            TimeSpan.FromMilliseconds(200),
            4_096);
        ProcessRunResult result;
        try
        {
            result = await runner.RunAsync(request, CancellationToken.None);
        }
        finally
        {
            stopwatch.Stop();
            await KillProcessIfAliveAsync(processId);
        }

        Assert.IsTrue(result.TimedOut);
        Assert.IsTrue(result.OutcomeUnknown);
        Assert.IsTrue(terminationRequested);
        Assert.IsTrue(
            stopwatch.Elapsed < TimeSpan.FromSeconds(3),
            $"Timeout boundary took {stopwatch.Elapsed}.");
    }

    [TestMethod]
    [Timeout(15_000, CooperativeCancellation = true)]
    public async Task RunAsyncDoesNotWaitIndefinitelyForInheritedOutputAfterParentExit()
    {
        using TemporaryDirectory temporary = new();
        string childProcessIdPath = Path.Combine(temporary.Path, "child.pid");
        ProcessRunner runner = new();
        ProcessRequest request = new(
            FindLockProbe(),
            ["exit-with-child-tree", childProcessIdPath],
            TimeSpan.FromSeconds(10),
            4_096);
        Stopwatch stopwatch = Stopwatch.StartNew();
        ProcessRunResult result;
        int childProcessId = 0;
        try
        {
            result = await runner.RunAsync(request, CancellationToken.None);
            stopwatch.Stop();
            childProcessId = await TryReadProcessIdAsync(childProcessIdPath);
        }
        finally
        {
            stopwatch.Stop();
            if (childProcessId == 0)
            {
                childProcessId = await TryReadProcessIdAsync(childProcessIdPath);
            }

            await KillProcessIfAliveAsync(childProcessId);
        }

        Assert.AreNotEqual(0, childProcessId);
        Assert.IsTrue(result.OutcomeUnknown);
        Assert.IsNotNull(result.LaunchError);
        Assert.Contains("could not be drained", result.LaunchError, StringComparison.Ordinal);
        Assert.IsTrue(
            stopwatch.Elapsed < TimeSpan.FromSeconds(10),
            $"Output-drain boundary took {stopwatch.Elapsed}.");
    }

    [TestMethod]
    [Timeout(15_000, CooperativeCancellation = true)]
    public async Task RunAsyncCancellationRemainsBoundedWhenTreeTerminationFails()
    {
        int processId = 0;
        bool terminationRequested = false;
        Stopwatch stopwatch = new();
        ProcessRunner runner = new(process =>
        {
            processId = process.Id;
            terminationRequested = true;
            stopwatch.Restart();
            return Task.FromResult(false);
        });
        ProcessRequest request = new(
            FindLockProbe(),
            ["hold-process"],
            TimeSpan.FromSeconds(30),
            4_096);
        using CancellationTokenSource cancellationSource = new(TimeSpan.FromMilliseconds(200));
        try
        {
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                runner.RunAsync(request, cancellationSource.Token));
        }
        finally
        {
            stopwatch.Stop();
            await KillProcessIfAliveAsync(processId);
        }

        Assert.IsTrue(terminationRequested);
        Assert.IsTrue(
            stopwatch.Elapsed < TimeSpan.FromSeconds(3),
            $"Cancellation boundary took {stopwatch.Elapsed}.");
    }

    [TestMethod]
    public async Task RunAsyncRejectsInvalidUtf8OutputWithoutLeakingRawBytes()
    {
        ProcessRunner runner = new();
        ProcessRequest request = new(
            FindLockProbe(),
            ["invalid-utf8"],
            TimeSpan.FromSeconds(10),
            4_096);

        ProcessRunResult result = await runner.RunAsync(request, CancellationToken.None);

        Assert.IsNotNull(result.LaunchError);
        Assert.Contains("not valid UTF-8", result.LaunchError, StringComparison.Ordinal);
        Assert.AreEqual(string.Empty, result.StandardOutput);
    }

    [TestMethod]
    public async Task RunAsyncDoesNotAutoDetectAUtf16BomInsteadOfTheUtf8Contract()
    {
        ProcessRunner runner = new();
        ProcessRequest request = new(
            FindLockProbe(),
            ["utf16-output"],
            TimeSpan.FromSeconds(10),
            4_096);

        ProcessRunResult result = await runner.RunAsync(request, CancellationToken.None);

        Assert.IsNotNull(result.LaunchError);
        Assert.Contains("not valid UTF-8", result.LaunchError, StringComparison.Ordinal);
    }

    private static string FindLockProbe()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(
                directory.FullName,
                "tests",
                "CortexCompanion.LockProbe",
                "bin",
                "Release",
                "net10.0-windows",
                "CortexCompanion.LockProbe.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("The built CortexCompanion.LockProbe.exe was not found.");
    }

    private static async Task AssertProcessExitedAsync(int processId)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                using Process process = Process.GetProcessById(processId);
                if (process.HasExited)
                {
                    return;
                }
            }
            catch (ArgumentException)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        Assert.Fail($"Descendant process {processId} survived tree termination.");
    }

    private static async Task<int> TryReadProcessIdAsync(string path)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                if (File.Exists(path) &&
                    int.TryParse(
                        await File.ReadAllTextAsync(path),
                        System.Globalization.CultureInfo.InvariantCulture,
                        out int processId))
                {
                    return processId;
                }
            }
            catch (IOException)
            {
                // The probe may still be atomically closing its process-id file.
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }

        return 0;
    }

    private static async Task KillProcessIfAliveAsync(int processId)
    {
        if (processId <= 0)
        {
            return;
        }

        try
        {
            using Process process = Process.GetProcessById(processId);
            if (process.HasExited)
            {
                return;
            }

            process.Kill(entireProcessTree: true);
            using CancellationTokenSource cleanupSource = new(TimeSpan.FromSeconds(5));
            await process.WaitForExitAsync(cleanupSource.Token);
        }
        catch (ArgumentException)
        {
        }
    }
}
