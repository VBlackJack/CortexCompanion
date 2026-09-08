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
    private readonly AsyncRelayCommand _cancel;
    private readonly AsyncRelayCommand _copy;
    private readonly Action<string> _copyText;
    private SourceChoice _source = Choices[0];
    private long _criteriaRevision;
    private bool _hasExecuted;
    private string _executedCriteria = string.Empty;
    private string _query = string.Empty;
    private string _section = string.Empty;
    private string _status = UiStrings.SearchReady;
    private SearchHit? _selected;
    private CancellationTokenSource? _cancellation;

    /// <summary>Creates a disabled screen until the compatible runtime is available.</summary>
    public SearchViewModel(SearchClient? client, Action<string>? copyText = null)
    {
        _client = client;
        _copyText = copyText ?? System.Windows.Clipboard.SetText;
        _copy = new AsyncRelayCommand(() =>
        {
            if (Selected is SearchHit hit)
            {
                _copyText(string.Join(Environment.NewLine + Environment.NewLine,
                    hit.Excerpt, hit.Title, hit.OpenTarget ?? hit.Path));
                Status = UiStrings.SearchCopied;
            }
            return Task.CompletedTask;
        }, () => Selected is not null);
        _copy.ExecutionFailed += (_, _) => Status = UiStrings.SearchCopyFailed;
        _status = client is null ? UiStrings.SearchUnavailable : UiStrings.SearchReady;
        _search = new AsyncRelayCommand(SearchAsync,
            () => _client is not null && !string.IsNullOrWhiteSpace(Query) && Query.Length <= SearchClient.QueryLimit);
        _search.ExecutionFailed += (_, _) => Status = UiStrings.SearchFailed;
        _open = new AsyncRelayCommand(OpenAsync, () => IsSafeTarget(Selected?.OpenTarget));
        _cancel = new AsyncRelayCommand(() => { Stop(); return Task.CompletedTask; }, () => _cancellation is not null);
    }

    /// <summary>Gets the bounded natural-language query.</summary>
    public string Query
    {
        get => _query;
        set { if (SetProperty(ref _query, value)) { CriteriaChanged(); _search.RaiseCanExecuteChanged(); } }
    }

    /// <summary>Gets an optional exact section filter.</summary>
    public string Section { get => _section; set { if (SetProperty(ref _section, value)) { CriteriaChanged(); } } }

    /// <summary>Gets the selected source domain filter.</summary>
    public SourceChoice Source { get => _source; set { if (SetProperty(ref _source, value)) { CriteriaChanged(); } } }

    /// <summary>Gets localized domain choices.</summary>
    public static IReadOnlyList<SourceChoice> Choices { get; } =
        [new("", UiStrings.SearchAll), new("note", UiStrings.SearchNotes), new("doc", UiStrings.SearchDocuments)];

    /// <summary>Gets the result list for the most recent successful request.</summary>
    public ObservableCollection<SearchHit> Results { get; } = [];

    /// <summary>Gets the selected result, opened only on an explicit user command.</summary>
    public SearchHit? Selected
    {
        get => _selected;
        set { if (SetProperty(ref _selected, value)) { _open.RaiseCanExecuteChanged(); _copy.RaiseCanExecuteChanged(); OnPropertyChanged(nameof(OpenStatus)); } }
    }

    /// <summary>Gets the accessible operation status.</summary>
    public string Status { get => _status; private set => SetProperty(ref _status, value); }

    /// <summary>Gets the query submission command.</summary>
    public ICommand SearchCommand => _search;

    /// <summary>Gets the explicit source-opening command.</summary>
    public ICommand OpenCommand => _open;

    /// <summary>Copies the selected excerpt and its reference after an explicit action.</summary>
    public ICommand CopyCommand => _copy;

    /// <summary>Gets the action that cancels only the current search.</summary>
    public ICommand CancelCommand => _cancel;

    /// <summary>Gets the immutable criteria of the last submitted request.</summary>
    public string ExecutedCriteria { get => _executedCriteria; private set => SetProperty(ref _executedCriteria, value); }

    /// <summary>Explains why the selected source cannot be opened without relaxing target restrictions.</summary>
    public string OpenStatus
    {
        get
        {
            string? target = Selected?.OpenTarget;
            if (Selected is null) { return string.Empty; }
            if (string.IsNullOrWhiteSpace(target)) { return UiStrings.SearchSourceUnavailable; }
            if (IsSafeTarget(target)) { return string.Empty; }
            if (Path.IsPathFullyQualified(target) && !target.StartsWith(@"\\", StringComparison.Ordinal) &&
                Path.GetExtension(target).ToLowerInvariant() is ".md" or ".pdf" or ".txt")
            {
                return UiStrings.SearchSourceMissing;
            }
            return UiStrings.SearchSourceBlocked;
        }
    }

    private void CriteriaChanged()
    {
        _criteriaRevision++;
        if (!_hasExecuted) { return; }
        Results.Clear();
        Selected = null;
        Status = UiStrings.SearchCriteriaChanged;
    }

    /// <summary>Cancels an obsolete runtime query when settings change.</summary>
    public void Stop() => _cancellation?.Cancel();

    private async Task SearchAsync()
    {
        Results.Clear();
        Selected = null;
        Status = UiStrings.SearchRunning;
        _hasExecuted = true;
        long revision = _criteriaRevision;
        ExecutedCriteria = UiStrings.FormatSearchExecuted(
            Query, string.IsNullOrWhiteSpace(Section) ? UiStrings.SearchAll : Section, Source.Label);
        using CancellationTokenSource cancellation = new();
        _cancellation = cancellation;
        _cancel.RaiseCanExecuteChanged();
        try
        {
            SearchResponse response = await _client!.SearchAsync(Query, Section, Source.Kind, cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            if (revision != _criteriaRevision) { Status = UiStrings.SearchCriteriaChanged; return; }
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
        finally
        {
            _cancellation = null;
            _cancel.RaiseCanExecuteChanged();
            if (revision != _criteriaRevision) { Status = UiStrings.SearchCriteriaChanged; }
        }
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
