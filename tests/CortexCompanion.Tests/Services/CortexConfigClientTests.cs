// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class CortexConfigClientTests
{
    private const string ValidGetJson = """
        {
          "contract_version": 1,
          "operation": "config_get",
          "status": "succeeded",
          "present": true,
          "content_hash": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
          "valid": true,
          "error": null,
          "values": {
            "schema_version": 1,
            "kb_path": "G:/Knowledge",
            "chroma_path": "G:/Knowledge/.cortex/chroma",
            "index_whole_folder": true,
            "included_sections": ["docs"],
            "excluded_dirs": [".git"],
            "exclude_files": ["private.md"],
            "max_markdown_file_size_bytes": 1048576,
            "max_pdf_size_bytes": 10485760,
            "write_lock_path": "G:/Knowledge/.cortex/write.lock",
            "write_lock_timeout_seconds": 30.0
          },
          "restart_required": false,
          "reindex_required": false
        }
        """;
    private static readonly string[] GetArguments = ["config", "get", "--json"];
    private static readonly string[] SetAbsentArguments =
        ["config", "set", "--json", "--expect-absent", "--kb-path", "G:/Knowledge"];

    [TestMethod]
    public async Task GetAsyncProjectsVersionedSnapshotAndUsesOnlyJsonCli()
    {
        StubProcessRunner runner = new(ProcessRunResult.Completed(0, ValidGetJson, string.Empty));
        CortexConfigClient client = new(runner);

        CortexConfigSnapshot snapshot = await client.GetAsync(@"C:\Cortex\cortex.exe");

        Assert.IsTrue(snapshot.IsValid);
        Assert.AreEqual("G:/Knowledge", snapshot.KnowledgeBasePath);
        Assert.IsNotNull(runner.LastRequest);
        CollectionAssert.AreEqual(
            GetArguments,
            runner.LastRequest.Arguments.ToArray());
    }

    [TestMethod]
    public async Task SetKnowledgeBasePathAsyncUsesAbsentCasAndProjectsSuccess()
    {
        const string Json = """
            {
              "contract_version": 1,
              "operation": "config_set",
              "status": "succeeded",
              "changed": true,
              "previous_content_hash": null,
              "content_hash": "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
              "backup_written": false,
              "rebuilt_from_defaults": false,
              "restart_required": true,
              "reindex_required": true,
              "error": null
            }
            """;
        StubProcessRunner runner = new(ProcessRunResult.Completed(0, Json, string.Empty));
        CortexConfigClient client = new(runner);

        CortexConfigMutationResult result = await client.SetKnowledgeBasePathAsync(
            @"C:\Cortex\cortex.exe",
            "G:/Knowledge",
            expectedContentHash: null,
            expectAbsent: true);

        Assert.AreEqual(CortexConfigMutationStatus.Succeeded, result.Status);
        Assert.IsTrue(result.ReindexRequired);
        Assert.IsNotNull(runner.LastRequest);
        CollectionAssert.AreEqual(
            SetAbsentArguments,
            runner.LastRequest.Arguments.ToArray());
    }

    [TestMethod]
    public async Task SetKnowledgeBasePathAsyncAcceptsSuccessfulNormalizationWithoutReindex()
    {
        const string Json = """
            {
              "contract_version": 1,
              "operation": "config_set",
              "status": "succeeded",
              "changed": true,
              "previous_content_hash": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
              "content_hash": "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
              "backup_written": true,
              "rebuilt_from_defaults": false,
              "restart_required": true,
              "reindex_required": false,
              "error": null
            }
            """;
        CortexConfigClient client = new(
            new StubProcessRunner(ProcessRunResult.Completed(0, Json, string.Empty)));

        CortexConfigMutationResult result = await client.SetKnowledgeBasePathAsync(
            @"C:\Cortex\cortex.exe",
            "G:/Knowledge",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            expectAbsent: false);

        Assert.AreEqual(CortexConfigMutationStatus.Succeeded, result.Status);
        Assert.IsFalse(result.ReindexRequired);
    }

    [TestMethod]
    public async Task SetKnowledgeBasePathAsyncAcceptsStructuredConflictOnNonzeroExit()
    {
        const string Json = """
            {
              "contract_version": 1,
              "operation": "config_set",
              "status": "conflict",
              "changed": false,
              "previous_content_hash": "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
              "content_hash": "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
              "backup_written": false,
              "rebuilt_from_defaults": false,
              "restart_required": false,
              "reindex_required": false,
              "error": { "code": "hash_mismatch", "phase": "compare", "path": null }
            }
            """;
        StubProcessRunner runner = new(ProcessRunResult.Completed(9, Json, string.Empty));
        CortexConfigClient client = new(runner);

        CortexConfigMutationResult result = await client.SetKnowledgeBasePathAsync(
            @"C:\Cortex\cortex.exe",
            "G:/Knowledge",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            expectAbsent: false);

        Assert.AreEqual(CortexConfigMutationStatus.Conflict, result.Status);
        Assert.AreEqual("hash_mismatch", result.Error?.Code);
    }

    [TestMethod]
    public async Task GetAsyncRejectsUnknownContractVersion()
    {
        string json = ValidGetJson.Replace(
            "\"contract_version\": 1",
            "\"contract_version\": 2",
            StringComparison.Ordinal);
        CortexConfigClient client = new(
            new StubProcessRunner(ProcessRunResult.Completed(0, json, string.Empty)));

        await Assert.ThrowsAsync<CortexCliContractException>(
            () => client.GetAsync(@"C:\Cortex\cortex.exe"));
    }

    [TestMethod]
    [DataRow("root_extra")]
    [DataRow("values_extra")]
    [DataRow("error_path")]
    [DataRow("unsafe_phase")]
    [DataRow("values_schema")]
    public async Task GetAsyncRejectsMutatedStrictJsonShape(string mutation)
    {
        string json = mutation switch
        {
            "root_extra" => ValidGetJson.Replace(
                "\"reindex_required\": false",
                "\"reindex_required\": false, \"extra\": true",
                StringComparison.Ordinal),
            "values_extra" => ValidGetJson.Replace(
                "\"write_lock_timeout_seconds\": 30.0",
                "\"write_lock_timeout_seconds\": 30.0, \"extra\": true",
                StringComparison.Ordinal),
            "error_path" => InvalidGetJson("G:/private"),
            "unsafe_phase" => InvalidGetJson(null).Replace(
                "\"phase\": \"validate\"",
                "\"phase\": \"validate\\nsecret\"",
                StringComparison.Ordinal),
            "values_schema" => ValidGetJson.Replace(
                "\"schema_version\": 1",
                "\"schema_version\": 2",
                StringComparison.Ordinal),
            _ => throw new InvalidOperationException("Unknown mutation."),
        };
        CortexConfigClient client = new(
            new StubProcessRunner(ProcessRunResult.Completed(0, json, string.Empty)));

        await Assert.ThrowsAsync<CortexCliContractException>(
            () => client.GetAsync(@"C:\Cortex\cortex.exe"));
    }

    [TestMethod]
    [DataRow("conflict", "hash_mismatch", 9, CortexConfigMutationStatus.Conflict)]
    [DataRow("locked", "locked", 2, CortexConfigMutationStatus.Locked)]
    [DataRow("failed", "invalid_argument", 6, CortexConfigMutationStatus.Failed)]
    [DataRow("failed", "write_failed", 1, CortexConfigMutationStatus.Failed)]
    public async Task SetKnowledgeBasePathAsyncAcceptsOnlyExactFailureExitMapping(
        string status,
        string code,
        int exitCode,
        CortexConfigMutationStatus expectedStatus)
    {
        string json = MutationFailureJson(status, code);
        CortexConfigClient client = new(
            new StubProcessRunner(ProcessRunResult.Completed(exitCode, json, string.Empty)));

        CortexConfigMutationResult result = await client.SetKnowledgeBasePathAsync(
            @"C:\Cortex\cortex.exe",
            "G:/Knowledge",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            expectAbsent: false);

        Assert.AreEqual(expectedStatus, result.Status);
        Assert.AreEqual(code, result.Error?.Code);
    }

    [TestMethod]
    public async Task SetKnowledgeBasePathAsyncRejectsWrongExitForOtherwiseValidFailure()
    {
        string json = MutationFailureJson("conflict", "hash_mismatch");
        CortexConfigClient client = new(
            new StubProcessRunner(ProcessRunResult.Completed(1, json, string.Empty)));

        await Assert.ThrowsAsync<CortexCliContractException>(() => client.SetKnowledgeBasePathAsync(
            @"C:\Cortex\cortex.exe",
            "G:/Knowledge",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            expectAbsent: false));
    }

    [TestMethod]
    [DataRow("backup_without_previous")]
    [DataRow("rebuilt_without_previous")]
    [DataRow("unchanged_hash_on_changed")]
    public async Task SetKnowledgeBasePathAsyncRejectsIncoherentSuccessFlags(string mutation)
    {
        string json = SuccessfulMutationJson();
        json = mutation switch
        {
            "backup_without_previous" => json.Replace(
                "\"backup_written\": false",
                "\"backup_written\": true",
                StringComparison.Ordinal),
            "rebuilt_without_previous" => json.Replace(
                "\"rebuilt_from_defaults\": false",
                "\"rebuilt_from_defaults\": true",
                StringComparison.Ordinal),
            "unchanged_hash_on_changed" => SuccessfulExistingMutationJson().Replace(
                "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                StringComparison.Ordinal),
            _ => throw new InvalidOperationException("Unknown mutation."),
        };
        CortexConfigClient client = new(
            new StubProcessRunner(ProcessRunResult.Completed(0, json, string.Empty)));

        await Assert.ThrowsAsync<CortexCliContractException>(() => client.SetKnowledgeBasePathAsync(
            @"C:\Cortex\cortex.exe",
            "G:/Knowledge",
            expectedContentHash: null,
            expectAbsent: true));
    }

    [TestMethod]
    public async Task GetAsyncExposesAnUnknownOutcomeAfterProcessTimeout()
    {
        CortexConfigClient client = new(
            new StubProcessRunner(ProcessRunResult.Timeout(string.Empty, string.Empty)));

        CortexCliContractException exception = await Assert.ThrowsAsync<CortexCliContractException>(
            () => client.GetAsync(@"C:\Cortex\cortex.exe"));

        Assert.IsTrue(exception.OutcomeUnknown);
    }

    private static string InvalidGetJson(string? path) => $$"""
        {
          "contract_version": 1,
          "operation": "config_get",
          "status": "succeeded",
          "present": true,
          "content_hash": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
          "valid": false,
          "error": { "code": "invalid_configuration", "phase": "validate", "path": {{(path is null ? "null" : $"\"{path}\"")}} },
          "values": null,
          "restart_required": false,
          "reindex_required": false
        }
        """;

    private static string MutationFailureJson(string status, string code) => $$"""
        {
          "contract_version": 1,
          "operation": "config_set",
          "status": "{{status}}",
          "changed": false,
          "previous_content_hash": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
          "content_hash": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
          "backup_written": false,
          "rebuilt_from_defaults": false,
          "restart_required": false,
          "reindex_required": false,
          "error": { "code": "{{code}}", "phase": "write", "path": null }
        }
        """;

    private static string SuccessfulMutationJson() => """
        {
          "contract_version": 1,
          "operation": "config_set",
          "status": "succeeded",
          "changed": true,
          "previous_content_hash": null,
          "content_hash": "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
          "backup_written": false,
          "rebuilt_from_defaults": false,
          "restart_required": true,
          "reindex_required": true,
          "error": null
        }
        """;

    private static string SuccessfulExistingMutationJson() => """
        {
          "contract_version": 1,
          "operation": "config_set",
          "status": "succeeded",
          "changed": true,
          "previous_content_hash": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
          "content_hash": "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
          "backup_written": true,
          "rebuilt_from_defaults": false,
          "restart_required": true,
          "reindex_required": false,
          "error": null
        }
        """;
}
