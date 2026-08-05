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
        StubProcessRunner runner = new(ProcessRunResult.Completed(0, AppConstants.MinSupportedCliVersion, string.Empty));
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
        StubProcessRunner runner = new(ProcessRunResult.Completed(0, "2026.0716.01\r\n", string.Empty));
        CliHandshakeService service = CreateService(runner);

        CliHandshakeResult result = await service.EvaluateAsync(new AppSettings(executablePath));

        Assert.AreEqual(CliHandshakeStatus.Compatible, result.Status);
        Assert.IsFalse(result.IsReadOnly);
        Assert.AreEqual(new CliVersion(2026, 7, 16, 1), result.DetectedVersion);
        Assert.IsNotNull(runner.LastRequest);
        CollectionAssert.AreEqual(
            new[] { AppConstants.CliVersionArgument },
            runner.LastRequest.Arguments.ToArray());
        Assert.AreEqual(executablePath, runner.LastRequest.FilePath);
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
