// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using CortexCompanion.Commands;
using CortexCompanion.Localization;
using CortexCompanion.Logging;
using CortexCompanion.Services;

namespace CortexCompanion.ViewModels;

/// <summary>Searches only the current runtime and never retains results after a failed request.</summary>
public sealed class SearchViewModel : ViewModelBase
{
    private readonly SearchClient? _client;
    private readonly AsyncRelayCommand _search;
    private readonly AsyncRelayCommand _open;
    private string _query = string.Empty;
    private string _section = string.Empty;
    private string _status = UiStrings.SearchReady;
    private SearchHit? _selected;
    private CancellationTokenSource? _cancellation;

    /// <summary>Creates a disabled screen until the compatible runtime is available.</summary>
    public SearchViewModel(SearchClient? client)
    {
        _client = client;
        _status = client is null ? UiStrings.SearchUnavailable : UiStrings.SearchReady;
        _search = new AsyncRelayCommand(SearchAsync,
            () => _client is not null && !string.IsNullOrWhiteSpace(Query) && Query.Length <= SearchClient.QueryLimit);
        _search.ExecutionFailed += (_, _) => Status = UiStrings.SearchFailed;
        _open = new AsyncRelayCommand(OpenAsync, () => IsSafeTarget(Selected?.OpenTarget));
    }

    /// <summary>Gets the bounded natural-language query.</summary>
    public string Query
    {
        get => _query;
        set { if (SetProperty(ref _query, value)) { _search.RaiseCanExecuteChanged(); } }
    }

    /// <summary>Gets an optional exact section filter.</summary>
    public string Section { get => _section; set => SetProperty(ref _section, value); }

    /// <summary>Gets the selected source domain filter.</summary>
    public SourceChoice Source { get; set; } = Choices[0];

    /// <summary>Gets localized domain choices.</summary>
    public static IReadOnlyList<SourceChoice> Choices { get; } =
        [new("", UiStrings.SearchAll), new("note", UiStrings.SearchNotes), new("doc", UiStrings.SearchDocuments)];

    /// <summary>Gets the result list for the most recent successful request.</summary>
    public ObservableCollection<SearchHit> Results { get; } = [];

    /// <summary>Gets the selected result, opened only on an explicit user command.</summary>
    public SearchHit? Selected
    {
        get => _selected;
        set { if (SetProperty(ref _selected, value)) { _open.RaiseCanExecuteChanged(); } }
    }

    /// <summary>Gets the accessible operation status.</summary>
    public string Status { get => _status; private set => SetProperty(ref _status, value); }

    /// <summary>Gets the query submission command.</summary>
    public ICommand SearchCommand => _search;

    /// <summary>Gets the explicit source-opening command.</summary>
    public ICommand OpenCommand => _open;

    /// <summary>Cancels an obsolete runtime query when settings change.</summary>
    public void Stop() => _cancellation?.Cancel();

    private async Task SearchAsync()
    {
        Results.Clear();
        Selected = null;
        Status = UiStrings.SearchRunning;
        using CancellationTokenSource cancellation = new();
        _cancellation = cancellation;
        try
        {
            SearchResponse response = await _client!.SearchAsync(Query, Section, Source.Kind, cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            foreach (SearchHit hit in response.Results) { Results.Add(hit); }
            Status = response.Degraded ? UiStrings.SearchDegraded :
                Results.Count == 0 ? UiStrings.SearchEmpty : UiStrings.SearchComplete;
        }
        catch (OperationCanceledException) { Status = UiStrings.SearchCancelled; }
        catch (TimeoutException) { Status = UiStrings.SearchTimeout; }
        catch (Exception exception) when (exception is IOException or InvalidDataException or
            System.Text.Json.JsonException or ArgumentException)
        {
            FileLogger.Error("Search response could not be read", exception);
            Status = UiStrings.SearchFailed;
        }
        finally { _cancellation = null; }
    }

    internal static bool IsSafeTarget(string? target)
    {
        if (string.IsNullOrWhiteSpace(target)) { return false; }
        if (Uri.TryCreate(target, UriKind.Absolute, out Uri? uri) && uri.Scheme == Uri.UriSchemeHttps)
        {
            return !string.IsNullOrEmpty(uri.Host) && string.IsNullOrEmpty(uri.UserInfo);
        }

        return Path.IsPathFullyQualified(target) && !target.StartsWith(@"\\", StringComparison.Ordinal) &&
            Path.GetExtension(target).ToLowerInvariant() is ".md" or ".pdf" or ".txt" && File.Exists(target);
    }

    private Task OpenAsync()
    {
        if (IsSafeTarget(Selected?.OpenTarget))
        {
            try { Process.Start(new ProcessStartInfo(Selected!.OpenTarget!) { UseShellExecute = true }); }
            catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or IOException)
            {
                FileLogger.Error("Search source could not be opened", exception);
                Status = UiStrings.SearchOpenFailed;
            }
        }

        return Task.CompletedTask;
    }
}

/// <summary>Pairs a stable source kind with its localized label.</summary>
public sealed record SourceChoice(string Kind, string Label);
