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
    public async Task EvaluateAsyncAcceptsTheFloorAndWhatFollowsItAndRefusesWhatPrecedesIt()
    {
        // Written against the constant rather than against a literal. Frozen literals are why
        // the floor sat a month behind the product: raising it broke seven tests that cared
        // about the boundary rather than about any particular version.
        using TemporaryDirectory temporaryDirectory = new();
        string executablePath = temporaryDirectory.CreateFakeCli();
        string floor = AppConstants.MinSupportedCliVersion;
        Assert.IsTrue(new CliVersionPolicy().TryParse(floor, out CliVersion parsed));
        // The day before the floor is earlier whatever the revision, which a revision
        // decrement is not once the floor ends in .00.
        string justBelow =
            $"{parsed.Year:D4}.{parsed.Month:D2}{parsed.Day - 1:D2}.99";
        string later = $"{parsed.Year:D4}.{parsed.Month:D2}{parsed.Day + 1:D2}.00";

        foreach (string version in new[] { floor, later })
        {
            StubProcessRunner runner = new(ProcessRunResult.Completed(0, version, string.Empty));
            CliHandshakeResult result = await CreateService(runner)
                .EvaluateAsync(new AppSettings(executablePath));

            Assert.AreEqual(CliHandshakeStatus.Compatible, result.Status, version);
            Assert.IsFalse(result.IsReadOnly, version);
        }

        // The version this floor exists to refuse: the one shipped immediately before it.
        StubProcessRunner refused = new(ProcessRunResult.Completed(0, justBelow, string.Empty));
        CliHandshakeResult refusal = await CreateService(refused)
            .EvaluateAsync(new AppSettings(executablePath));

        Assert.AreEqual(CliHandshakeStatus.IncompatibleVersion, refusal.Status, justBelow);
        Assert.IsTrue(refusal.IsReadOnly, justBelow);
    }

    [TestMethod]
    [DataRow("2026.0716.01", CliHandshakeStatus.IncompatibleVersion, true, 2026, 7, 16, 1)]
    [DataRow("2026.0805.00", CliHandshakeStatus.IncompatibleVersion, true, 2026, 8, 5, 0)]
    [DataRow("2026.0807.99", CliHandshakeStatus.IncompatibleVersion, true, 2026, 8, 7, 99)]
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
        // The floor moves with each paired release, so the fixture follows the constant
        // rather than a literal that would have to be edited every time.
        StubProcessRunner runner = new(ProcessRunResult.Completed(
            0, AppConstants.MinSupportedCliVersion + "\r\n", string.Empty));
        CliHandshakeService service = CreateService(runner);

        CliHandshakeResult result = await service.EvaluateAsync(new AppSettings(executablePath));

        Assert.AreEqual(CliHandshakeStatus.Compatible, result.Status);
        Assert.IsFalse(result.IsReadOnly);
        Assert.IsTrue(new CliVersionPolicy().TryParse(
            AppConstants.MinSupportedCliVersion, out CliVersion expectedVersion));
        Assert.AreEqual(expectedVersion, result.DetectedVersion);
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
        StubProcessRunner runner = new(ProcessRunResult.Completed(0, AppConstants.MinSupportedCliVersion, string.Empty));
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
