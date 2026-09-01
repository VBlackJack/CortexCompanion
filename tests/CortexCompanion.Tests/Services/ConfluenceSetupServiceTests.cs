// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;
using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

/// <summary>Guards novice-safe first-run Confluence configuration creation.</summary>
[TestClass]
public sealed class ConfluenceSetupServiceTests
{
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
        ConfluenceSetupService service = new(store, TimeProvider.System);
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

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now) => _now = now;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
