// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

/// <summary>Checks the section-root contract against documents captured from the real Cortex CLI.</summary>
[TestClass]
public sealed class SectionRootsClientTests
{
    private const string CliPath = @"C:\Cortex\cortex.exe";
    private const string FixtureFolder = "SectionRoots";
    private static readonly string[] RootsArguments = ["config", "roots", "--json"];
    private static readonly TimeSpan ConfiguredTimeout = TimeSpan.FromSeconds(90);

    [TestMethod]
    public async Task RootsFixtureProjectsAMountedSectionWhoseFolderIsMissing()
    {
        StubProcessRunner runner = Runner(0, Fixture("config-roots-missing.json"));

        SectionRootsSnapshot snapshot = await new CortexConfigClient(runner).GetSectionRootsAsync(CliPath, ConfiguredTimeout);

        Assert.IsTrue(snapshot.IsValid);
        Assert.IsTrue(snapshot.Present);
        Assert.IsFalse(snapshot.IndexWholeFolder);
        Assert.IsNull(snapshot.Error);
        Assert.AreEqual(64, snapshot.ContentHash?.Length);
        Assert.HasCount(2, snapshot.Sections);
        Assert.AreEqual(new SectionRoot("_memory", snapshot.Sections[0].Root, true, false), snapshot.Sections[0]);
        Assert.IsTrue(snapshot.Sections[1].Exists);
        Assert.IsFalse(snapshot.Sections[1].Mounted);
        Assert.IsNotNull(runner.LastRequest);
        CollectionAssert.AreEqual(RootsArguments, runner.LastRequest.Arguments.ToArray());
        Assert.AreEqual(ConfiguredTimeout, runner.LastRequest.Timeout);
    }

    [TestMethod]
    public async Task InvalidRootsFixtureProjectsAnEmptyListAndTheStructuredError()
    {
        SectionRootsSnapshot snapshot = await new CortexConfigClient(Runner(0, Fixture("config-roots-invalid.json")))
            .GetSectionRootsAsync(CliPath, ConfiguredTimeout);

        Assert.IsFalse(snapshot.IsValid);
        Assert.IsEmpty(snapshot.Sections);
        Assert.AreEqual(new CortexCliError("invalid_configuration", "validate"), snapshot.Error);
    }

    [TestMethod]
    [DataRow("\"exists\": false", "\"exists\": false, \"extra\": 1")]
    [DataRow("\"contract_version\": 1", "\"contract_version\": 2")]
    [DataRow("\"operation\": \"config_roots\"", "\"operation\": \"config_get\"")]
    [DataRow("\"valid\": true", "\"valid\": false")]
    [DataRow("\"mounted\": true", "\"mounted\": null")]
    [DataRow("\"index_whole_folder\": false", "\"index_whole_folder\": null")]
    public async Task RootsDocumentThatBreaksTheContractIsRefused(string original, string replacement)
    {
        string document = Fixture("config-roots-missing.json");
        Assert.Contains(original, document, StringComparison.Ordinal);
        CortexConfigClient client = new(Runner(0, document.Replace(original, replacement, StringComparison.Ordinal)));

        await Assert.ThrowsAsync<CortexCliContractException>(
            () => client.GetSectionRootsAsync(CliPath, ConfiguredTimeout));
    }

    [TestMethod]
    public async Task RootsNonzeroExitIsRefusedEvenWithAValidDocument()
    {
        CortexConfigClient client = new(Runner(2, Fixture("config-roots-missing.json")));

        await Assert.ThrowsAsync<CortexCliContractException>(
            () => client.GetSectionRootsAsync(CliPath, ConfiguredTimeout));
    }

    private static string Fixture(string name) => CliFixtures.Read(FixtureFolder, name);

    private static StubProcessRunner Runner(int exitCode, string output) =>
        new(ProcessRunResult.Completed(exitCode, output, string.Empty));
}
