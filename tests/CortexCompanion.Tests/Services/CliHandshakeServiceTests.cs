// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;
using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class CliHandshakeServiceTests
{
    [TestMethod]
    public async Task EvaluateAsyncCliNotConfiguredFailsClosedWithoutProcessCall()
    {
        StubProcessRunner runner = new(ProcessRunResult.Completed(0, "2026.0808.00", string.Empty));
        CliHandshakeService service = CreateService(runner);

        CliHandshakeResult result = await service.EvaluateAsync(AppSettings.Empty);

        Assert.AreEqual(CliHandshakeStatus.NotConfigured, result.Status);
        Assert.IsTrue(result.IsReadOnly);
        Assert.AreEqual(0, runner.CallCount);
    }

    [TestMethod]
    public async Task EvaluateAsyncOlderVersionFailsClosed()
    {
        await AssertStatusAsync(
            ProcessRunResult.Completed(0, "2026.0715.99\r\n", string.Empty),
            CliHandshakeStatus.IncompatibleVersion);
    }

    [TestMethod]
    [DataRow("2026.0716.01", CliHandshakeStatus.IncompatibleVersion, true, 2026, 7, 16, 1)]
    [DataRow("2026.0805.00", CliHandshakeStatus.IncompatibleVersion, true, 2026, 8, 5, 0)]
    [DataRow("2026.0807.99", CliHandshakeStatus.IncompatibleVersion, true, 2026, 8, 7, 99)]
    [DataRow("2026.0808.00", CliHandshakeStatus.Compatible, false, 2026, 8, 8, 0)]
    [DataRow("2026.0808.01", CliHandshakeStatus.Compatible, false, 2026, 8, 8, 1)]
    public async Task EvaluateAsyncVersionGateRejectsPreReleaseAndAcceptsReleaseOrLater(
        string version,
        CliHandshakeStatus expectedStatus,
        bool expectedReadOnly,
        int year,
        int month,
        int day,
        int revision)
    {
        using TemporaryDirectory temporaryDirectory = new();
        string executablePath = temporaryDirectory.CreateFakeCli();
        StubProcessRunner runner = new(ProcessRunResult.Completed(0, version, string.Empty));
        CliHandshakeService service = CreateService(runner);

        CliHandshakeResult result = await service.EvaluateAsync(new AppSettings(executablePath));

        Assert.AreEqual(expectedStatus, result.Status);
        Assert.AreEqual(expectedReadOnly, result.IsReadOnly);
        Assert.AreEqual(new CliVersion(year, month, day, revision), result.DetectedVersion);
        Assert.AreEqual(1, runner.CallCount);
    }

    [TestMethod]
    public async Task EvaluateAsyncUnparseableVersionFailsClosed()
    {
        await AssertStatusAsync(
            ProcessRunResult.Completed(0, "cortex version unknown\r\n", string.Empty),
            CliHandshakeStatus.UnparseableVersion);
    }

    [TestMethod]
    public async Task EvaluateAsyncTimeoutFailsClosed()
    {
        await AssertStatusAsync(
            ProcessRunResult.Timeout(string.Empty, string.Empty),
            CliHandshakeStatus.TimedOut);
    }

    [TestMethod]
    public async Task EvaluateAsyncNonZeroExitCodeFailsClosed()
    {
        await AssertStatusAsync(
            ProcessRunResult.Completed(3, string.Empty, "sanitized failure"),
            CliHandshakeStatus.NonZeroExitCode);
    }

    [TestMethod]
    public async Task EvaluateAsyncCompatibleVersionAllowsNormalModeAndUsesVersionArgument()
    {
        using TemporaryDirectory temporaryDirectory = new();
        string executablePath = temporaryDirectory.CreateFakeCli();
        StubProcessRunner runner = new(ProcessRunResult.Completed(0, "2026.0808.00\r\n", string.Empty));
        CliHandshakeService service = CreateService(runner);

        CliHandshakeResult result = await service.EvaluateAsync(new AppSettings(executablePath));

        Assert.AreEqual(CliHandshakeStatus.Compatible, result.Status);
        Assert.IsFalse(result.IsReadOnly);
        Assert.AreEqual(new CliVersion(2026, 8, 8, 0), result.DetectedVersion);
        Assert.IsNotNull(runner.LastRequest);
        CollectionAssert.AreEqual(
            new[] { AppConstants.CliVersionArgument },
            runner.LastRequest.Arguments.ToArray());
        Assert.AreEqual(executablePath, runner.LastRequest.FilePath);
        Assert.AreEqual(
            TimeSpan.FromSeconds(AppConstants.DefaultCliTimeoutSeconds),
            runner.LastRequest.Timeout);
    }

    [TestMethod]
    [DataRow(15)]
    [DataRow(30)]
    [DataRow(60)]
    [DataRow(120)]
    public async Task EvaluateAsyncUsesConfiguredBoundedHandshakeTimeout(int timeoutSeconds)
    {
        using TemporaryDirectory temporaryDirectory = new();
        string executablePath = temporaryDirectory.CreateFakeCli();
        StubProcessRunner runner = new(ProcessRunResult.Completed(0, "2026.0808.00", string.Empty));
        CliHandshakeService service = CreateService(runner);

        CliHandshakeResult result = await service.EvaluateAsync(
            new AppSettings(executablePath, timeoutSeconds));

        Assert.AreEqual(CliHandshakeStatus.Compatible, result.Status);
        Assert.IsNotNull(runner.LastRequest);
        Assert.AreEqual(TimeSpan.FromSeconds(timeoutSeconds), runner.LastRequest.Timeout);
    }

    private static CliHandshakeService CreateService(StubProcessRunner runner) =>
        new(new CliVersionPolicy(), runner);

    private static async Task AssertStatusAsync(ProcessRunResult processResult, CliHandshakeStatus expectedStatus)
    {
        using TemporaryDirectory temporaryDirectory = new();
        string executablePath = temporaryDirectory.CreateFakeCli();
        StubProcessRunner runner = new(processResult);
        CliHandshakeService service = CreateService(runner);

        CliHandshakeResult result = await service.EvaluateAsync(new AppSettings(executablePath));

        Assert.AreEqual(expectedStatus, result.Status);
        Assert.IsTrue(result.IsReadOnly);
        Assert.AreEqual(1, runner.CallCount);
    }
}
