// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows.Input;
using CortexCompanion.Commands;
using CortexCompanion.Localization;
using CortexCompanion.Models;
using CortexCompanion.Services;

namespace CortexCompanion.ViewModels;

/// <summary>Offers one link-first source workflow with recoverable connection and configuration steps.</summary>
public sealed partial class PagesViewModel
{
    private readonly ConfluenceSourceService? _sourceService;
    private readonly AsyncRelayCommand _inspectSourceCommand;
    private string _sourceUrl = string.Empty;
    private bool _needsCredential;
    private bool _needsSpaceKey;
    private bool _sourceAdded;

    /// <summary>Gets the one shared link draft, retained through authentication and preview failures.</summary>
    public string SourceUrl
    {
        get => _sourceUrl;
        set
        {
            if (!SetProperty(ref _sourceUrl, value)) { return; }
            SourceAdded = false;
            SetupPageUrl = value;
            _inspectSourceCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(ShowSourceClassification));
            OnPropertyChanged(nameof(SourceIdentity));
        }
    }

    /// <summary>Gets the inferred identity without claiming a remote title before preview.</summary>
    public string SourceIdentity
    {
        get
        {
            try
            {
                ConfluencePageUrlAnalysis analysis = ConfluencePageUrlAnalyzer.Analyze(SourceUrl);
                return string.Join(" / ", new[] { analysis.BaseUrl, analysis.InferredSpaceKey }.Where(value => value is not null));
            }
            catch (ConfluenceSetupValidationException) { return string.Empty; }
        }
    }

    /// <summary>Gets whether the source form can be edited during a network operation.</summary>
    public bool CanEditSource => !IsBusy;

    /// <summary>Gets whether the inline credential help is needed.</summary>
    public bool NeedsCredential
    {
        get => _needsCredential;
        private set
        {
            if (SetProperty(ref _needsCredential, value)) { OnPropertyChanged(nameof(ShowSourceDetails)); }
        }
    }

    /// <summary>Gets whether the URL lacks a usable space identity.</summary>
    public bool NeedsSpaceKey { get => _needsSpaceKey; private set => SetProperty(ref _needsSpaceKey, value); }

    /// <summary>Gets whether setup or authentication details must be displayed.</summary>
    public bool ShowSourceDetails => NeedsCredential;

    /// <summary>Gets whether this link points at a new space whose classification must be chosen.</summary>
    public bool ShowSourceClassification => NeedsConfluenceConfiguration ||
        !Spaces.Any(space => string.Equals(space.SpaceKey, InferSpaceKeyOrNull(SourceUrl), StringComparison.OrdinalIgnoreCase));

    /// <summary>Gets whether the last explicit selection was committed, independently of collection.</summary>
    public bool SourceAdded { get => _sourceAdded; private set => SetProperty(ref _sourceAdded, value); }

    /// <summary>Measures the link, presents scope choices, then saves the confirmed selection.</summary>
    public ICommand InspectSourceCommand => _inspectSourceCommand;

    private async Task InspectSourceAsync()
    {
        if (_sourceService is null) { return; }
        if (HasOverrides) { StateMessage = UiStrings.FlowOverrides; return; }
        try
        {
            ConfluencePageUrlAnalysis analysis = ConfluencePageUrlAnalyzer.Analyze(SourceUrl);
            NeedsSpaceKey = analysis.InferredSpaceKey is null;
            if (NeedsSpaceKey && string.IsNullOrWhiteSpace(SetupSpaceKey))
            {
                StateMessage = UiStrings.FlowNeedSpace;
                return;
            }
            if (NeedsConfluenceConfiguration && SetupExpiryDate is null)
            {
                NeedsCredential = true;
                StateMessage = UiStrings.FlowConnectFirst;
                return;
            }
            IsBusy = true;
            StateMessage = UiStrings.FlowInspecting;
            bool added = await _sourceService.AddAsync(new(SourceUrl,
                analysis.InferredSpaceKey ?? SetupSpaceKey,
                SetupExpiryDate is DateTime date ? ToEndOfLocalDay(date) : default,
                SetupConverterPath, SelectedClassification.Code), IsReadOnly, CancellationToken.None);
            await RefreshAsync();
            SourceAdded = added;
            if (added) { NeedsCredential = false; }
            StateMessage = added ? UiStrings.FlowAdded : UiStrings.FlowCancelled;
        }
        catch (ConfluenceCliOperationException exception)
        {
            NeedsCredential = exception.ExitCode == CortexExitCode.Auth;
            StateMessage = NeedsCredential ? UiStrings.FlowAuthenticationRequired :
                FormatCliFailure(exception.ExitCode, exception.Message, false, null);
        }
        catch (ConfluenceConfigConflictException) { StateMessage = UiStrings.PagesCasConflict; }
        catch (Exception exception) when (exception is ConfluenceSetupValidationException or PageMutationRejectedException or
            ConfluenceConfigValidationException or ConfluenceConfigLockedException or ConfluenceConfigMutationException or IOException)
        {
            StateMessage = exception.Message;
        }
        finally { IsBusy = false; }
    }
}
