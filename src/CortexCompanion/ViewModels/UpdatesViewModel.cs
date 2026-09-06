// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;
using System.Windows.Input;
using CortexCompanion.Commands;
using CortexCompanion.Localization;
using CortexCompanion.Models;
using CortexCompanion.Services;

namespace CortexCompanion.ViewModels;

/// <summary>Separates installed versions, online availability and explicit installer navigation.</summary>
public sealed class UpdatesViewModel : ViewModelBase
{
    private readonly ReleaseUpdateClient _client;
    private string _cortexVersion = UiStrings.ValueUnknown;
    private string _availableVersion = UiStrings.ValueUnknown;
    private string _status = UiStrings.UpdateNotChecked;
    private CliVersion? _latest;

    /// <summary>Creates an offline-safe screen; no network request is made at startup.</summary>
    public UpdatesViewModel(ReleaseUpdateClient client)
    {
        _client = client;
        AsyncRelayCommand check = new(CheckAsync);
        check.ExecutionFailed += (_, _) => { AvailableVersion = UiStrings.ValueUnknown; Status = UiStrings.UpdateCheckFailed; };
        CheckCommand = check;
        AsyncRelayCommand open = new(() =>
        {
            Process.Start(new ProcessStartInfo(ReleaseUpdateClient.ReleasesPage) { UseShellExecute = true });
            return Task.CompletedTask;
        });
        open.ExecutionFailed += (_, _) => Status = UiStrings.UpdateOpenFailed;
        OpenReleaseCommand = open;
    }

    /// <summary>Gets the actual running Companion assembly version.</summary>
    public string CompanionVersion { get; } = CompanionVersionProvider.GetCurrent();

    /// <summary>Gets the CLI version observed by the current handshake.</summary>
    public string CortexVersion
    {
        get => _cortexVersion;
        set { if (SetProperty(ref _cortexVersion, value) && _latest is not null) { UpdateStatus(); } }
    }

    /// <summary>Gets the last successfully checked stable version.</summary>
    public string AvailableVersion { get => _availableVersion; private set => SetProperty(ref _availableVersion, value); }
    /// <summary>Gets the accessible outcome, including network failures.</summary>
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    /// <summary>Checks availability only after an explicit action.</summary>
    public ICommand CheckCommand { get; }
    /// <summary>Opens the official common-installer release page.</summary>
    public ICommand OpenReleaseCommand { get; }

    private async Task CheckAsync()
    {
        _latest = null;
        AvailableVersion = UiStrings.ValueUnknown;
        Status = UiStrings.UpdateChecking;
        _latest = await _client.ReadLatestAsync(CancellationToken.None);
        AvailableVersion = _latest.Value.ToString();
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        CliVersionPolicy policy = new();
        Status = !policy.TryParse(CompanionVersion, out CliVersion companion) ||
            !policy.TryParse(CortexVersion, out CliVersion cortex) ? UiStrings.UpdateCompareUnknown :
            companion < _latest!.Value || cortex < _latest.Value ? UiStrings.UpdateAvailable :
            companion != cortex ? UiStrings.UpdateVersionsDiffer : UiStrings.UpdateCurrent;
    }
}
