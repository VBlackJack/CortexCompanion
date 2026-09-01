// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class ConfluenceCliClientTests
{
    private static readonly TimeSpan ConfiguredTimeout = TimeSpan.FromSeconds(120);

    [TestMethod]
    public void MapExitCodeImplementsCompleteFrozenTable()
    {
        CortexExitCode[] expected =
        [
            CortexExitCode.Ok,
            CortexExitCode.Error,
            CortexExitCode.Locked,
            CortexExitCode.NotDue,
            CortexExitCode.Auth,
            CortexExitCode.Remote,
            CortexExitCode.InvalidInput,
            CortexExitCode.NotFound,
            CortexExitCode.OutsideAllowlist,
        ];

        for (int code = 0; code <= 8; code++)
        {
            Assert.AreEqual(expected[code], ConfluenceCliClient.MapExitCode(code));
        }
    }

    [TestMethod]
    public async Task GetPagesAlwaysPassesAbsoluteConfigBeforeSubcommand()
    {
        StubProcessRunner runner = new(ProcessRunResult.Completed(
            0,
            """{"contract_version":1,"spaces":[],"last_sync":{"last_success_at":null,"status":null,"error_code":null}}""",
            string.Empty));
        string cliPath = Path.GetFullPath(@"C:\tools\cortex.exe");
        string configPath = Path.GetFullPath(@"C:\config\confluence.toml");
        ConfluenceCliClient client = new(runner, cliPath, configPath, ConfiguredTimeout);

        ConfluenceCliResult<PagesContract> result = await client.GetPagesAsync(CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        CollectionAssert.AreEqual(
            new[] { "confluence", "--config", configPath, "pages", "--json" },
            runner.LastRequest!.Arguments.ToArray());
        Assert.AreEqual(ConfiguredTimeout, runner.LastRequest.Timeout);
    }

    [TestMethod]
    public async Task NonzeroResolveDoesNotParseStdout()
    {
        StubProcessRunner runner = new(ProcessRunResult.Completed(7, "{not-json", "page absente"));
        ConfluenceCliClient client = new(
            runner,
            @"C:\tools\cortex.exe",
            @"C:\config\confluence.toml",
            ConfiguredTimeout);

        ConfluenceCliResult<ResolvedPageContract> result = await client.ResolveAsync("123", CancellationToken.None);

        Assert.AreEqual(CortexExitCode.NotFound, result.ExitCode);
        Assert.IsNull(result.Value);
        Assert.AreEqual("page absente", result.StandardError);
    }
}
