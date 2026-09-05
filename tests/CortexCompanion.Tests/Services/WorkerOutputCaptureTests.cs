// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text;
using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class WorkerOutputCaptureTests
{
    [TestMethod]
    public async Task WriteFailureDrainsTheRemainingInput()
    {
        using StringReader source = new(new string('x', 20_000));
        bool captured = await WorkerOutputCapture.CopyAsync(source, () => new FailingWriter());
        Assert.IsFalse(captured);
        Assert.AreEqual(-1, source.Read());
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task ScheduledCaptureFailureDoesNotBlockEitherPipe(bool failStandardOutput)
    {
        using TemporaryDirectory temporary = new();
        string normal = Path.Combine(temporary.Path, "captured.log");
        ScheduledProcessRunner runner = new();
        ScheduledProcessResult result = await runner.RunAsync(
            FindProbe(), ["sync", "--json"],
            failStandardOutput ? temporary.Path : normal,
            failStandardOutput ? normal : temporary.Path).WaitAsync(TimeSpan.FromSeconds(15));
        Assert.AreEqual(0, result.ExitCode);
        Assert.AreEqual(WorkerOutputCapture.FailureKind, result.LaunchError);
        Assert.AreEqual(1_000_000L, new FileInfo(normal).Length);
    }

    [TestMethod]
    public async Task DetachedCaptureFailurePersistsATerminalResult()
    {
        using TemporaryDirectory temporary = new();
        string run = Path.Combine(temporary.Path, "run");
        Directory.CreateDirectory(Path.Combine(run, "stderr.log"));
        int exitCode = await SyncWorker.ExecuteAsync(new SyncWorkerArguments(
            run, FindProbe(), SyncRunKind.LocalDocuments, null, false))
            .WaitAsync(TimeSpan.FromSeconds(15));
        Assert.AreEqual(1, exitCode);
        SyncWorkerResult? result = await SyncRunPersistence.ReadJsonAsync<SyncWorkerResult>(
            Path.Combine(run, SyncRunPersistence.ResultFileName), CancellationToken.None);
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.ExitCode);
        Assert.AreEqual(WorkerOutputCapture.FailureKind, result.LaunchError);
    }

    private static string FindProbe()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string path = Path.Combine(directory.FullName,
                "tests", "CortexCompanion.LockProbe", "bin", "Release",
                "net10.0-windows", "CortexCompanion.LockProbe.exe");
            if (File.Exists(path))
            {
                return path;
            }
            directory = directory.Parent;
        }
        throw new FileNotFoundException("The built lock probe was not found.");
    }

    private sealed class FailingWriter : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;

        public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default) =>
            Task.FromException(new IOException("Injected disk write failure."));
    }
}
