// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Collections.ObjectModel;
using System.Windows.Input;
using CortexCompanion.Commands;
using CortexCompanion.Localization;
using CortexCompanion.Services;

namespace CortexCompanion.ViewModels;

/// <summary>Presents bounded historical evidence and explicit recovery navigation.</summary>
public sealed class HistoryViewModel : ViewModelBase
{
    private readonly OperationHistoryReader _reader;
    private string _status = UiStrings.HistoryScope;
    /// <summary>Creates a history that loads only on request.</summary>
    public HistoryViewModel(OperationHistoryReader reader)
    {
        _reader = reader;
        AsyncRelayCommand refresh = new(RefreshAsync);
        refresh.ExecutionFailed += (_, _) => { Entries.Clear(); Status = UiStrings.HistoryReadFailed; };
        RefreshCommand = refresh;
    }
    /// <summary>Gets retained records, newest first.</summary>
    public ObservableCollection<OperationHistoryEntry> Entries { get; } = [];
    /// <summary>Gets the coverage or failure message.</summary>
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    /// <summary>Reloads historical records without running a synchronization.</summary>
    public ICommand RefreshCommand { get; }

    private async Task RefreshAsync()
    {
        Status = UiStrings.HistoryLoading;
        IReadOnlyList<OperationHistoryEntry> entries = await _reader.ReadAsync(CancellationToken.None);
        Entries.Clear();
        foreach (OperationHistoryEntry entry in entries) { Entries.Add(entry); }
        Status = entries.Count == 0 ? UiStrings.HistoryEmpty : UiStrings.HistoryScope;
    }
}
