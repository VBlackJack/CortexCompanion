// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Commands;
using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;
using CortexCompanion.ViewModels;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class SearchClientTests
{
    [TestMethod]
    [DataRow(4, 2, false)]
    [DataRow(6, 0, true)]
    [DataRow(7, 0, true)]
    public void SearchCapabilityHasItsOwnVersionBoundary(int day, int revision, bool expected)
    {
        CliHandshakeResult handshake = new(CliHandshakeStatus.Compatible, new CliVersion(2026, 9, day, revision));
        Assert.AreEqual(expected, CompanionRuntimeFactory.SupportsSearch(handshake));
        Assert.IsFalse(handshake.IsReadOnly);
        Assert.IsFalse(CompanionRuntimeFactory.SupportsSearch(handshake with { Status = CliHandshakeStatus.LaunchFailed }));
    }

    [TestMethod]
    [DataRow("query")]
    [DataRow("section")]
    [DataRow("source")]
    public async Task EditedCriteriaDiscardLateResultsAndKeepExecutedCriteria(string field)
    {
        DelayedRunner runner = new();
        SearchViewModel viewModel = new(new SearchClient(runner, "cortex.exe", TimeSpan.FromSeconds(30))) { Query = "original" };
        Task search = ((AsyncRelayCommand)viewModel.SearchCommand).ExecuteAsync(null);
        if (field == "query") { viewModel.Query = "changed"; }
        if (field == "section") { viewModel.Section = "changed"; }
        if (field == "source") { viewModel.Source = SearchViewModel.Choices[1]; }
        runner.Completion.SetResult(ProcessRunResult.Completed(0, Valid, ""));
        await search;
        Assert.IsEmpty(viewModel.Results);
        Assert.AreEqual(UiStrings.SearchCriteriaChanged, viewModel.Status);
        Assert.Contains("original", viewModel.ExecutedCriteria, StringComparison.Ordinal);
        Assert.IsTrue(viewModel.SearchCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task CancelPropagatesAndRejectsEvenAnUncooperativeLateResponse()
    {
        DelayedRunner runner = new();
        SearchViewModel viewModel = new(new SearchClient(runner, "cortex.exe", TimeSpan.FromSeconds(30))) { Query = "original" };
        Task search = ((AsyncRelayCommand)viewModel.SearchCommand).ExecuteAsync(null);
        Assert.IsTrue(viewModel.CancelCommand.CanExecute(null));
        await ((AsyncRelayCommand)viewModel.CancelCommand).ExecuteAsync(null);
        Assert.IsTrue(runner.Token.IsCancellationRequested);
        runner.Completion.SetResult(ProcessRunResult.Completed(0, Valid, ""));
        await search;
        Assert.IsEmpty(viewModel.Results);
        Assert.AreEqual(UiStrings.SearchCancelled, viewModel.Status);
        Assert.IsFalse(viewModel.CancelCommand.CanExecute(null));
        Assert.IsTrue(viewModel.SearchCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task EditingCompletedSearchClearsItsResultsAndSelection()
    {
        SearchViewModel viewModel = new(new SearchClient(new StubProcessRunner(ProcessRunResult.Completed(0, Valid, "")),
            "cortex.exe", TimeSpan.FromSeconds(30)))
        { Query = "original" };
        await ((AsyncRelayCommand)viewModel.SearchCommand).ExecuteAsync(null);
        viewModel.Selected = viewModel.Results[0];
        Assert.AreEqual(UiStrings.SearchSourceUnavailable, viewModel.OpenStatus);
        viewModel.Query = "changed";
        Assert.IsEmpty(viewModel.Results);
        Assert.IsNull(viewModel.Selected);
    }

    [TestMethod]
    public void RestrictedAndMissingSourcesExplainTheDisabledAction()
    {
        SearchViewModel viewModel = new(null);
        viewModel.Selected = new("id", "title", "text", "path", "", "note", "", "javascript:alert(1)");
        Assert.AreEqual(UiStrings.SearchSourceBlocked, viewModel.OpenStatus);
        using TemporaryDirectory temporary = new();
        viewModel.Selected = viewModel.Selected with { OpenTarget = Path.Combine(temporary.Path, "missing.md") };
        Assert.AreEqual(UiStrings.SearchSourceMissing, viewModel.OpenStatus);
        Assert.IsFalse(viewModel.OpenCommand.CanExecute(null));
    }

    private sealed class DelayedRunner : IProcessRunner
    {
        public TaskCompletionSource<ProcessRunResult> Completion { get; } = new();
        public CancellationToken Token { get; private set; }
        public Task<ProcessRunResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken)
        {
            Token = cancellationToken;
            return Completion.Task;
        }
    }

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
