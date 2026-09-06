// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Localization;

namespace CortexCompanion.ViewModels;

public sealed partial class PagesViewModel
{
    private bool _reconnectingExisting;
    private Func<Task>? _retryAfterReconnect;
    /// <summary>Hides addition fields while reconnecting an existing source.</summary>
    public bool ShowSourceLink => !_reconnectingExisting;
    /// <summary>Names addition and credential recovery separately.</summary>
    public string SourceFormTitle => _reconnectingExisting ? UiStrings.SourcesReconnect : UiStrings.SourcesAdd;
    /// <summary>Explains the active form without requiring a duplicate source link.</summary>
    public string SourceFormHelp => _reconnectingExisting ? UiStrings.FlowConnectFirst : UiStrings.FlowIntro;

    private void BeginReconnect()
    {
        _reconnectingExisting = HasConfluenceConfiguration;
        _retryAfterReconnect = _retryAction;
        NeedsCredential = true;
        ShowAddSource = true;
        NotifyReconnect();
    }

    private void NotifyReconnect()
    {
        OnPropertyChanged(nameof(ShowSourceLink));
        OnPropertyChanged(nameof(SourceFormTitle));
        OnPropertyChanged(nameof(SourceFormHelp));
    }

    /// <summary>Validates the expiry before replacing the Windows credential.</summary>
    public bool PrepareSourceCredential()
    {
        if (!_reconnectingExisting || SetupExpiryDate is DateTime date && ToEndOfLocalDay(date) > DateTimeOffset.UtcNow) { return true; }
        SetSourceError(UiStrings.FlowConnectFirst);
        return false;
    }

    /// <summary>Resumes the failed workflow after saving expiry, without adding the source again.</summary>
    public async Task CompleteSourceCredentialAsync()
    {
        if (!_reconnectingExisting) { await InspectSourceAsync(); return; }
        if (SetupExpiryDate is not DateTime date || _mutations is null) { SetSourceError(UiStrings.FlowConnectFirst); return; }
        Func<Task>? resume = _retryAfterReconnect;
        await RunMutationAsync(() => _mutations.UpdateCredentialExpiryAsync(ToEndOfLocalDay(date), IsReadOnly, CancellationToken.None));
        if (HasSourceError) { return; }
        _reconnectingExisting = false;
        NeedsCredential = false;
        ShowAddSource = false;
        NotifyReconnect();
        if (resume is not null) { await resume(); }
    }
}
