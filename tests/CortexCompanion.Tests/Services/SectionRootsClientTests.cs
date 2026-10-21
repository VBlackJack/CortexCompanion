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
    private const string Hash = "ecd5293bc424c73d8468032241a778f2563ab289df4cd5b42e63094c04c46d73";
    private static readonly string[] RootsArguments = ["config", "roots", "--json"];
    private static readonly string[] SetRootArguments =
        ["config", "set-root", "_memory", @"D:\Moved\_memory", "--json", "--expected-hash", Hash];
    private static readonly string[] ClearRootArguments =
        ["config", "clear-root", "_memory", "--json", "--expected-hash", Hash];
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

    [TestMethod]
    public async Task SetRootBuildsTheCasCommandAndProjectsTheRealSuccess()
    {
        StubProcessRunner runner = Runner(0, Fixture("config-set-root-succeeded.json"));

        SectionRootMutationResult result = await new CortexConfigClient(runner).SetSectionRootAsync(
            CliPath, "_memory", @"D:\Moved\_memory", Hash, ConfiguredTimeout);

        Assert.AreEqual(CortexConfigMutationStatus.Succeeded, result.Status);
        Assert.IsTrue(result.Changed);
        Assert.IsTrue(result.RestartRequired);
        Assert.IsNotNull(result.Root);
        Assert.IsNull(result.Error);
        Assert.IsNotNull(runner.LastRequest);
        CollectionAssert.AreEqual(
            SetRootArguments,
            runner.LastRequest.Arguments.ToArray());
        Assert.AreEqual(ConfiguredTimeout, runner.LastRequest.Timeout);
    }

    [TestMethod]
    public async Task ClearRootBuildsTheCasCommandAndProjectsTheRealSuccess()
    {
        StubProcessRunner runner = Runner(0, Fixture("config-clear-root-succeeded.json"));

        SectionRootMutationResult result = await new CortexConfigClient(runner).ClearSectionRootAsync(
            CliPath, "_memory", Hash, ConfiguredTimeout);

        Assert.AreEqual(CortexConfigMutationStatus.Succeeded, result.Status);
        Assert.IsNull(result.Root);
        Assert.IsTrue(result.RestartRequired);
        Assert.IsNotNull(runner.LastRequest);
        CollectionAssert.AreEqual(
            ClearRootArguments,
            runner.LastRequest.Arguments.ToArray());
    }

    [TestMethod]
    [DataRow("config-set-root-unchanged.json", 0, "_memory", false, CortexConfigMutationStatus.Unchanged, null)]
    [DataRow("config-clear-root-unchanged.json", 0, "_memory", true, CortexConfigMutationStatus.Unchanged, null)]
    [DataRow("config-set-root-conflict.json", 9, "_memory", false, CortexConfigMutationStatus.Conflict, "hash_mismatch")]
    [DataRow("config-set-root-invalid-argument.json", 6, "unknown", false, CortexConfigMutationStatus.Failed, "invalid_argument")]
    public async Task RealMutationOutcomesProjectTheirStatusAndError(
        string fixture,
        int exitCode,
        string section,
        bool clear,
        CortexConfigMutationStatus expectedStatus,
        string? expectedError)
    {
        CortexConfigClient client = new(Runner(exitCode, Fixture(fixture)));

        SectionRootMutationResult result = clear
            ? await client.ClearSectionRootAsync(CliPath, section, Hash, ConfiguredTimeout)
            : await client.SetSectionRootAsync(CliPath, section, @"D:\Moved", Hash, ConfiguredTimeout);

        Assert.AreEqual(expectedStatus, result.Status);
        Assert.IsFalse(result.Changed);
        Assert.IsFalse(result.RestartRequired);
        Assert.AreEqual(expectedError, result.Error?.Code);
    }

    [TestMethod]
    [DataRow("config-set-root-succeeded.json", 1, "_memory")]
    [DataRow("config-set-root-conflict.json", 1, "_memory")]
    [DataRow("config-set-root-succeeded.json", 0, "knowledge")]
    public async Task MutationWhoseExitCodeOrSectionDisagreesIsRefused(string fixture, int exitCode, string section)
    {
        CortexConfigClient client = new(Runner(exitCode, Fixture(fixture)));

        await Assert.ThrowsAsync<CortexCliContractException>(
            () => client.SetSectionRootAsync(CliPath, section, @"D:\Moved", Hash, ConfiguredTimeout));
    }

    [TestMethod]
    public async Task ClearRootRefusesADocumentThatReportsASetRoot()
    {
        CortexConfigClient client = new(Runner(0, Fixture("config-set-root-succeeded.json")));

        await Assert.ThrowsAsync<CortexCliContractException>(
            () => client.ClearSectionRootAsync(CliPath, "_memory", Hash, ConfiguredTimeout));
    }

    private static string Fixture(string name) => CliFixtures.Read(FixtureFolder, name);

    private static StubProcessRunner Runner(int exitCode, string output) =>
        new(ProcessRunResult.Completed(exitCode, output, string.Empty));
}
