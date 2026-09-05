// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Commands;
using CortexCompanion.Localization;
using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;
using CortexCompanion.ViewModels;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class SearchClientTests
{
    private const string Valid = """
        {"contract_version":1,"operation":"search","status":"succeeded","mode":"hybrid",
         "degraded":true,"results":[{"id":"a","title":"Title","excerpt":"Answer","path":"a.md",
         "section":"ops","source_kind":"note","updated_at":"2026-09-05T00:00:00Z","open_target":null}]}
        """;

    [TestMethod]
    public async Task FiltersRemainSeparateArgumentsAndDegradationIsVisible()
    {
        StubProcessRunner runner = new(ProcessRunResult.Completed(0, Valid, ""));
        SearchClient client = new(runner, "cortex.exe", TimeSpan.FromSeconds(30));
        SearchResponse response = await client.SearchAsync("quote \" and & text", "ops", "note", CancellationToken.None);
        Assert.IsTrue(response.Degraded);
        Assert.IsNotNull(runner.LastRequest);
        Assert.AreEqual("quote \" and & text", runner.LastRequest.Arguments[1]);
        Assert.AreEqual("note", runner.LastRequest.Arguments[^1]);
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(0)]
    public async Task UnsupportedEnvelopeIsNotAnEmptySuccess(int contractVersion)
    {
        StubProcessRunner runner = new(ProcessRunResult.Completed(0,
            Valid.Replace("\"contract_version\":1", $"\"contract_version\":{contractVersion}", StringComparison.Ordinal), ""));
        SearchClient client = new(runner, "cortex.exe", TimeSpan.FromSeconds(30));
        await Assert.ThrowsAsync<InvalidDataException>(() => client.SearchAsync("query", "", "", CancellationToken.None));
    }

    [TestMethod]
    public async Task TimeoutHasAnActionableState()
    {
        SearchViewModel viewModel = new(new SearchClient(
            new StubProcessRunner(ProcessRunResult.Timeout("", "")), "cortex.exe", TimeSpan.FromSeconds(30)))
        { Query = "test" };
        await ((AsyncRelayCommand)viewModel.SearchCommand).ExecuteAsync(null);
        Assert.AreEqual(UiStrings.SearchTimeout, viewModel.Status);
        Assert.IsEmpty(viewModel.Results);
    }

    [TestMethod]
    public async Task FailureClearsPreviousResults()
    {
        SearchViewModel viewModel = new(new SearchClient(
            new StubProcessRunner(ProcessRunResult.Completed(1, "", "failed")),
            "cortex.exe", TimeSpan.FromSeconds(30)))
        { Query = "test" };
        viewModel.Results.Add(new("old", "Old", "obsolete", "old.md", "", "note", "", null));
        await ((AsyncRelayCommand)viewModel.SearchCommand).ExecuteAsync(null);
        Assert.IsEmpty(viewModel.Results);
        Assert.AreEqual(UiStrings.SearchFailed, viewModel.Status);
    }

    [TestMethod]
    public async Task MissingContractFieldsAreRejected()
    {
        SearchClient client = new(new StubProcessRunner(ProcessRunResult.Completed(0,
            Valid.Replace("\"degraded\":true,", "", StringComparison.Ordinal), "")),
            "cortex.exe", TimeSpan.FromSeconds(30));
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() =>
            client.SearchAsync("query", "", "", CancellationToken.None));
    }

    [TestMethod]
    [DataRow("javascript:alert(1)")]
    [DataRow("file:///C:/Windows/System32/cmd.exe")]
    [DataRow("https://user:secret@example.org/a")]
    [DataRow("\\\\server\\share\\a.md")]
    public void UnsafeOpeningTargetsAreRejected(string target) => Assert.IsFalse(SearchViewModel.IsSafeTarget(target));
}
