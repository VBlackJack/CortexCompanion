// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;
using CortexCompanion.Localization;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

public sealed partial class PagesMutationService
{
    private sealed record RemovalUndo(ConfluenceConfiguration Before, string AfterHash);
    private RemovalUndo? _removalUndo;

    /// <summary>Gets whether this session retains a removal that can be reversed through CAS.</summary>
    public bool CanUndoRemoval => _removalUndo is not null;

    /// <summary>Gets the explicit apply choice from the last successful editor save.</summary>
    public bool LastSaveRequestsUpdate { get; private set; }

    /// <summary>Updates only the declared expiry after the user reconnects their credential.</summary>
    public async Task<bool> UpdateCredentialExpiryAsync(DateTimeOffset expiresAt, bool isReadOnly, CancellationToken cancellationToken)
    {
        EnsureMutable(isReadOnly);
        if (expiresAt <= DateTimeOffset.UtcNow) { throw new PageMutationRejectedException(UiStrings.ConfluenceSetupExpiredAuthentication); }
        ConfluenceConfigSnapshot snapshot = await _configStore.ReadAsync(cancellationToken);
        await WriteOrRefreshAsync(snapshot.Configuration with { AuthExpiresAt = expiresAt }, snapshot.ContentHash, cancellationToken);
        return true;
    }

    /// <summary>Restores a removal only if no configuration byte changed since it.</summary>
    public async Task<bool> UndoRemovalAsync(bool isReadOnly, CancellationToken cancellationToken)
    {
        EnsureMutable(isReadOnly);
        RemovalUndo? undo = _removalUndo;
        if (undo is null) { return false; }
        try { await WriteOrRefreshAsync(undo.Before, undo.AfterHash, cancellationToken); }
        catch (ConfluenceConfigRefreshRequiredException) { _removalUndo = null; throw; }
        _removalUndo = null;
        return true;
    }

    /// <summary>Edits a snapshot, validates new references, and commits only its confirmed replacement.</summary>
    public async Task<bool> EditSelectionAsync(string spaceKey, bool isReadOnly, CancellationToken cancellationToken)
    {
        EnsureMutable(isReadOnly);
        ConfluenceConfigSnapshot snapshot = await _configStore.ReadAsync(cancellationToken);
        ConfluenceConfiguration configuration = snapshot.Configuration.MigrateToVersionTwo();
        ConfluenceSpaceConfiguration before = FindSpace(configuration, spaceKey);
        ConfluenceCliResult<PagesContract> listing = await _cliClient.GetPagesAsync(cancellationToken);
        if (!listing.IsSuccess || listing.Value is null)
        {
            throw new ConfluenceCliOperationException(
                listing.ExitCode,
                listing.StandardError,
                listing.TimedOut);
        }

        IReadOnlyList<ConfiguredPageContract> pages = listing.Value.Spaces
            .SingleOrDefault(item => string.Equals(item.SpaceKey, spaceKey, StringComparison.OrdinalIgnoreCase))?.Pages
            ?? Array.Empty<ConfiguredPageContract>();
        LastSaveRequestsUpdate = false;
        SourceCatalogContract? catalog = null;
        async Task<ConfluenceCliResult<SourceCatalogContract>> LoadCatalog()
        {
            ConfluenceCliResult<SourceCatalogContract> result = await _cliClient.GetCatalogAsync(spaceKey, cancellationToken);
            if (result.IsSuccess && result.Value?.SpaceKey == spaceKey) { catalog = result.Value; }
            return result;
        }
        SourceSelectionEdit? edit = _confirmations.EditSelectionWithCatalog(before, pages, LoadCatalog);
        if (edit is null)
        {
            return false;
        }

        if (!Enum.IsDefined(edit.Selection) || edit.PageIds.Any(id => !before.PageIds.Contains(id, StringComparer.Ordinal) && catalog?.Pages.Any(page => page.PageId == id) != true))
        {
            throw new PageMutationRejectedException(UiStrings.PagesRejectPageNotConfigured);
        }

        List<string> ids = edit.PageIds.Distinct(StringComparer.Ordinal).ToList();
        if (edit.Selection != ConfluenceSelection.WholeSpace && !string.IsNullOrWhiteSpace(edit.AdditionalReference))
        {
            ConfluencePageUrlAnalysis analysis = ConfluencePageUrlAnalyzer.Analyze(edit.AdditionalReference);
            if (!string.Equals(configuration.BaseUrl, analysis.BaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                throw new PageMutationRejectedException(UiStrings.PagesRejectSpaceForeignBaseUrl);
            }

            ConfluenceCliResult<ResolvedPageContract> resolved = await _cliClient.ResolveAsync(edit.AdditionalReference, cancellationToken);
            if (!resolved.IsSuccess || resolved.Value is null)
            {
                throw new ConfluenceCliOperationException(
                    resolved.ExitCode,
                    resolved.StandardError,
                    resolved.TimedOut);
            }

            if (!string.Equals(spaceKey, resolved.Value.SpaceKey, StringComparison.OrdinalIgnoreCase))
            {
                throw new PageMutationRejectedException(UiStrings.PagesRejectPageNotConfigured);
            }

            if (!ids.Contains(resolved.Value.PageId, StringComparer.Ordinal))
            {
                ids.Add(resolved.Value.PageId);
            }
        }

        if (edit.Selection != ConfluenceSelection.WholeSpace && ids.Count == 0)
        {
            throw new PageMutationRejectedException(UiStrings.ManageEmpty);
        }

        ConfluenceSpaceConfiguration after = before with
        {
            Selection = edit.Selection,
            PageIds = edit.Selection == ConfluenceSelection.WholeSpace ? Array.Empty<string>() : ids.ToArray(),
        };
        if (before.Selection == after.Selection && before.PageIds.SequenceEqual(after.PageIds))
        {
            return false;
        }

        if (catalog is null) { _ = await LoadCatalog(); }
        if (!_confirmations.ConfirmSelectionReview(SourceChangeReview.Create(before, after, catalog, pages)))
        {
            return false;
        }

        await WriteOrRefreshAsync(configuration.MigrateToSchema(after.Selection == ConfluenceSelection.Subtree ? 3 : 2)
            .ReplaceSpace(after), snapshot.ContentHash, cancellationToken);
        LastSaveRequestsUpdate = edit.UpdateNow;
        return true;
    }

    /// <summary>Removes the source configuration without deleting remote content.</summary>
    public async Task<bool> RemoveSourceAsync(string spaceKey, bool isReadOnly, CancellationToken cancellationToken)
    {
        EnsureMutable(isReadOnly);
        ConfluenceConfigSnapshot snapshot = await _configStore.ReadAsync(cancellationToken);
        _ = FindSpace(snapshot.Configuration, spaceKey);
        if (!_confirmations.ConfirmRemoveSource(spaceKey))
        {
            return false;
        }

        ConfluenceConfigSnapshot saved = await WriteOrRefreshAsync(snapshot.Configuration.MigrateToVersionTwo().RemoveSpace(spaceKey), snapshot.ContentHash, cancellationToken);
        _removalUndo = new(snapshot.Configuration, saved.ContentHash);
        return true;
    }

    private async Task<bool?> CheckRemainingCoverageAsync(
        ConfluenceConfiguration configuration, ConfluenceSpaceConfiguration space,
        string pageId, CancellationToken cancellationToken)
    {
        if (_candidateClient is null)
        {
            return null;
        }

        string temporaryPath = Path.Combine(Path.GetTempPath(), "cortex-source-review-" + Guid.NewGuid().ToString("N") + ".toml");
        try
        {
            ConfluenceConfiguration candidate = configuration.ReplaceSpace(space with
            {
                PageIds = space.PageIds.Where(id => id != pageId).ToArray(),
            });
            await File.WriteAllBytesAsync(temporaryPath, ConfluenceConfigRenderer.Render(candidate), cancellationToken);
            ConfluenceCliResult<ResolvedPageContract> result = await _candidateClient(temporaryPath).ResolveAsync(pageId, cancellationToken);
            return result.IsSuccess ? result.Value?.Configured : null;
        }
        catch (IOException)
        {
            return null;
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    /// <summary>Opens a configured source on its configured HTTP server.</summary>
    public async Task OpenSourceAsync(string spaceKey, string? pageId, CancellationToken cancellationToken)
    {
        ConfluenceConfigSnapshot snapshot = await _configStore.ReadAsync(cancellationToken);
        _ = FindSpace(snapshot.Configuration, spaceKey);
        string suffix = pageId is null
            ? "/display/" + Uri.EscapeDataString(spaceKey)
            : "/pages/viewpage.action?pageId=" + Uri.EscapeDataString(pageId);
        if (!Uri.TryCreate(snapshot.Configuration.BaseUrl?.TrimEnd('/') + suffix, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new PageMutationRejectedException(UiStrings.PagesRejectSpaceForeignBaseUrl);
        }

        _ = Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }
}
