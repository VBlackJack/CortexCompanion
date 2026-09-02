// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;
using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

/// <summary>Guards novice-safe first-run Confluence configuration creation.</summary>
[TestClass]
public sealed class ConfluenceSetupServiceTests
{
    [TestMethod]
    public void ConverterDefaultFollowsTheValidatedCortexInstallationDirectory()
    {
        string cliPath = Path.Combine(
            "C:\\Users\\Fixture\\AppData\\Local\\Programs\\Cortex",
            AppConstants.CliExecutableName);

        string result = ConfluenceConverterPathResolver.ResolveDefault(cliPath);

        Assert.AreEqual(
            Path.Combine(
                Path.GetDirectoryName(cliPath)!,
                "Converters",
                AppConstants.ConfluenceConverterExecutableName),
            result);
    }

    [TestMethod]
    [DataRow(
        "https://kazan.example.test/spaces/DOC/pages/1001/Run+Book",
        "https://kazan.example.test",
        "DOC")]
    [DataRow(
        "https://kazan.example.test/wiki/spaces/DOC/pages/1001/Run+Book?src=share",
        "https://kazan.example.test/wiki",
        "DOC")]
    [DataRow(
        "https://kazan.example.test/wiki/display/RUN/Run+Book",
        "https://kazan.example.test/wiki",
        "RUN")]
    [DataRow(
        "https://kazan.example.test/wiki/pages/viewpage.action?pageId=1001",
        "https://kazan.example.test/wiki",
        null)]
    [DataRow(
        "https://kazan.example.test/wiki/x/AbC",
        "https://kazan.example.test/wiki",
        null)]
    public void AnalyzeSupportsEveryCortexPageUrlShape(
        string pageUrl,
        string expectedBaseUrl,
        string? expectedSpaceKey)
    {
        ConfluencePageUrlAnalysis result = ConfluencePageUrlAnalyzer.Analyze(pageUrl);

        Assert.AreEqual(expectedBaseUrl, result.BaseUrl);
        Assert.AreEqual(expectedSpaceKey, result.InferredSpaceKey);
    }

    [TestMethod]
    [DataRow("https://user@kazan.example.test/spaces/DOC/pages/1001")]
    [DataRow("https://kazan.example.test/spaces/DOC/overview")]
    [DataRow("https://kazan.example.test/wiki")]
    [DataRow("not-a-url")]
    public void AnalyzeRejectsUnsafeOrUnsupportedReferences(string pageUrl)
    {
        Assert.Throws<ConfluenceSetupValidationException>(() =>
            ConfluencePageUrlAnalyzer.Analyze(pageUrl));
    }

    [TestMethod]
    public async Task InitializeCreatesValidatedEmptyAllowlistWithoutAnySecret()
    {
        using TemporaryDirectory temporary = new();
        string configPath = Path.Combine(temporary.Path, "confluence.toml");
        string converterPath = Path.Combine(temporary.Path, "ConfluenceRAGBuilder.Console.exe");
        await File.WriteAllBytesAsync(converterPath, [0x4d, 0x5a]);
        ConfluenceConfigStore store = new(configPath);
        ConfluenceSetupService service = CreateService(store, converterPath);
        ConfluenceSetupRequest request = new(
            "https://kazan.example.test/wiki/spaces/DOC/pages/1001/Run+Book",
            "DOC",
            new DateTimeOffset(2099, 12, 31, 23, 59, 59, TimeSpan.FromHours(1)),
            converterPath,
            "pro-confidentiel");

        ConfluenceConfigSnapshot result = await service.InitializeAsync(request, CancellationToken.None);
        string persisted = await File.ReadAllTextAsync(configPath);

        Assert.AreEqual(2, result.Configuration.SchemaVersion);
        Assert.AreEqual("https://kazan.example.test/wiki", result.Configuration.BaseUrl);
        Assert.AreEqual(AppConstants.DefaultConfluenceCredentialTarget, result.Configuration.CredentialTarget);
        Assert.AreEqual(Path.GetFullPath(converterPath), result.Configuration.ConsolePath);
        Assert.HasCount(1, result.Configuration.Spaces);
        Assert.AreEqual("confluence/DOC", result.Configuration.Spaces[0].Target);
        Assert.AreEqual(ConfluenceSelection.Pages, result.Configuration.Spaces[0].Selection);
        Assert.IsEmpty(result.Configuration.Spaces[0].PageIds);
        Assert.DoesNotContain("pat =", persisted, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token =", persisted, StringComparison.OrdinalIgnoreCase);
        Assert.IsFalse(File.Exists(configPath + ".bak"));
    }

    [TestMethod]
    public async Task InitializeRejectsExpiredAuthenticationWithoutCreatingTheFile()
    {
        using TemporaryDirectory temporary = new();
        string configPath = Path.Combine(temporary.Path, "confluence.toml");
        ConfluenceSetupService service = new(
            new ConfluenceConfigStore(configPath),
            new FixedTimeProvider(new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero)));
        ConfluenceSetupRequest request = new(
            "https://kazan.example.test/spaces/DOC/pages/1001",
            "DOC",
            new DateTimeOffset(2026, 9, 1, 9, 59, 59, TimeSpan.Zero),
            null,
            "pro-confidentiel");

        await Assert.ThrowsAsync<ConfluenceSetupValidationException>(() =>
            service.InitializeAsync(request, CancellationToken.None));

        Assert.IsFalse(File.Exists(configPath));
    }

    [TestMethod]
    public async Task InitializeRejectsMissingConverterWhenOneWasEntered()
    {
        using TemporaryDirectory temporary = new();
        string configPath = Path.Combine(temporary.Path, "confluence.toml");
        ConfluenceSetupService service = new(new ConfluenceConfigStore(configPath), TimeProvider.System);
        ConfluenceSetupRequest request = new(
            "https://kazan.example.test/spaces/DOC/pages/1001",
            "DOC",
            new DateTimeOffset(2099, 12, 31, 23, 59, 59, TimeSpan.Zero),
            Path.Combine(temporary.Path, "missing.exe"),
            "pro-confidentiel");

        await Assert.ThrowsAsync<ConfluenceSetupValidationException>(() =>
            service.InitializeAsync(request, CancellationToken.None));

        Assert.IsFalse(File.Exists(configPath));
    }

    [TestMethod]
    public async Task EnsureReadyMigratesMissingConsolePathToValidatedEmbeddedConverter()
    {
        using TemporaryDirectory temporary = new();
        string configPath = Path.Combine(temporary.Path, "confluence.toml");
        string converterPath = Path.Combine(temporary.Path, "ConfluenceRAGBuilder.Console.exe");
        await File.WriteAllBytesAsync(converterPath, [0x4d, 0x5a]);
        ConfluenceConfigStore store = new(configPath);
        ConfluenceConfiguration legacy = new(
            2,
            "https://kazan.example.test/wiki",
            AppConstants.DefaultConfluenceCredentialTarget,
            new DateTimeOffset(2099, 12, 31, 23, 59, 59, TimeSpan.Zero),
            null,
            50,
            0.1,
            [new ConfluenceSpaceConfiguration(
                "DOC",
                "confluence/DOC",
                "pro-confidentiel",
                ConfluenceSelection.Pages,
                [])]);
        _ = await store.WriteAsync(legacy, expectedHash: null, CancellationToken.None);
        ConfluenceSetupService service = CreateService(store, converterPath);

        ConfluenceConfigSnapshot migrated = await service.EnsureReadyAsync(CancellationToken.None);

        Assert.AreEqual(Path.GetFullPath(converterPath), migrated.Configuration.ConsolePath);
        Assert.IsTrue(File.Exists(configPath + ".bak"));
        Assert.Contains("console_path =", await File.ReadAllTextAsync(configPath));
    }

    [TestMethod]
    public async Task ValidateConverterRejectsAWindowedOrHangingExecutableWithoutPersistingIt()
    {
        using TemporaryDirectory temporary = new();
        string converterPath = Path.Combine(temporary.Path, "ConfluenceRAGBuilder.exe");
        await File.WriteAllBytesAsync(converterPath, [0x4d, 0x5a]);
        StubProcessRunner runner = new(ProcessRunResult.Timeout(string.Empty, string.Empty));
        ConfluenceSetupService service = new(
            new ConfluenceConfigStore(Path.Combine(temporary.Path, "confluence.toml")),
            new ConfluenceConverterProbe(runner),
            converterPath);

        ConfluenceSetupValidationException exception = await Assert.ThrowsAsync<
            ConfluenceSetupValidationException>(() =>
                service.ValidateConverterAsync(converterPath, CancellationToken.None));

        Assert.AreEqual(UiStrings.ConfluenceSetupIncompatibleConverter, exception.Message);
        Assert.AreEqual("--probe", runner.LastRequest?.Arguments.Single());
        Assert.AreEqual(TimeSpan.FromSeconds(5), runner.LastRequest?.Timeout);
    }

    [TestMethod]
    public async Task EnsureReadyReplacesTheKnownWindowedExecutableWithEmbeddedConsole()
    {
        using TemporaryDirectory temporary = new();
        string configPath = Path.Combine(temporary.Path, "confluence.toml");
        string guiPath = Path.Combine(temporary.Path, AppConstants.ConfluenceWindowedExecutableName);
        string consolePath = Path.Combine(
            temporary.Path,
            AppConstants.ConfluenceConverterExecutableName);
        await File.WriteAllBytesAsync(guiPath, [0x4d, 0x5a]);
        await File.WriteAllBytesAsync(consolePath, [0x4d, 0x5a]);
        ConfluenceConfigStore store = new(configPath);
        ConfluenceConfiguration configuration = new(
            2,
            "https://kazan.example.test/wiki",
            AppConstants.DefaultConfluenceCredentialTarget,
            new DateTimeOffset(2099, 12, 31, 23, 59, 59, TimeSpan.Zero),
            guiPath,
            50,
            0.1,
            [new ConfluenceSpaceConfiguration(
                "DOC",
                "confluence/DOC",
                "pro-confidentiel",
                ConfluenceSelection.Pages,
                [])]);
        _ = await store.WriteAsync(configuration, expectedHash: null, CancellationToken.None);
        ConfluenceSetupService service = new(
            store,
            new ConfluenceConverterProbe(new PathAwareConverterRunner(consolePath)),
            consolePath);

        ConfluenceConfigSnapshot repaired = await service.EnsureReadyAsync(CancellationToken.None);

        Assert.AreEqual(Path.GetFullPath(consolePath), repaired.Configuration.ConsolePath);
        Assert.IsTrue(File.Exists(configPath + ".bak"));
    }

    private static ConfluenceSetupService CreateService(
        IConfluenceConfigStore store,
        string converterPath)
    {
        StubProcessRunner runner = new(ProcessRunResult.Completed(
            0,
            "{\"tool_version\":\"1.2.0\",\"schema_version\":1}",
            string.Empty));
        return new ConfluenceSetupService(
            store,
            new ConfluenceConverterProbe(runner),
            converterPath,
            timeProvider: TimeProvider.System);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now) => _now = now;

        public override DateTimeOffset GetUtcNow() => _now;
    }

    private sealed class PathAwareConverterRunner(string validPath) : IProcessRunner
    {
        public Task<ProcessRunResult> RunAsync(
            ProcessRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ProcessRunResult result = string.Equals(
                request.FilePath,
                Path.GetFullPath(validPath),
                StringComparison.OrdinalIgnoreCase)
                ? ProcessRunResult.Completed(
                    0,
                    "{\"tool_version\":\"1.2.0\",\"schema_version\":1}",
                    string.Empty)
                : ProcessRunResult.Timeout(string.Empty, string.Empty);
            return Task.FromResult(result);
        }
    }

    [TestMethod]
    [DataRow("http://wiki.example.test/spaces/DOC/pages/1001/Page")]
    [DataRow("http://10.0.0.5:8090/spaces/DOC/pages/1001/Page")]
    public void AnalyzeRefusesACleartextRemoteOrigin(string pageUrl)
    {
        // The PAT rides every request as a bearer header, so the user must be
        // told at paste time, not after a rejected write.
        ConfluenceSetupValidationException failure =
            Assert.ThrowsExactly<ConfluenceSetupValidationException>(
                () => ConfluencePageUrlAnalyzer.Analyze(pageUrl));

        Assert.AreEqual(UiStrings.ConfluenceSetupInsecurePageUrl, failure.Message);
    }

    [TestMethod]
    [DataRow("https://wiki.example.test/spaces/DOC/pages/1001/Page")]
    [DataRow("http://localhost:8090/spaces/DOC/pages/1001/Page")]
    [DataRow("http://127.0.0.1:8090/spaces/DOC/pages/1001/Page")]
    public void AnalyzeAcceptsTlsAndLoopbackOrigins(string pageUrl)
    {
        ConfluencePageUrlAnalysis analysis = ConfluencePageUrlAnalyzer.Analyze(pageUrl);

        StringAssert.StartsWith(analysis.BaseUrl, pageUrl[..pageUrl.IndexOf("/spaces", StringComparison.Ordinal)]);
        Assert.AreEqual("DOC", analysis.InferredSpaceKey);
    }
}
