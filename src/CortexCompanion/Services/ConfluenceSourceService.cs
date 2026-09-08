// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;
using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Previews a source with isolated non-secret settings, then performs one confirmed CAS write.</summary>
public sealed class ConfluenceSourceService(
    IConfluenceConfigStore store,
    ConfluenceSetupService setup,
    Func<string, IConfluenceCliClient> previewClient,
    IPageMutationConfirmationService confirmations)
{
    /// <summary>Gets the explicit apply choice after the last successful save.</summary>
    public bool LastSaveRequestsUpdate { get; private set; }

    /// <summary>Gets whether the last successful save merged the page into a source that already collected it.</summary>
    public bool LastSaveMerged { get; private set; }

    /// <summary>Adds the measured selection atomically; cancellation leaves no empty allowlist entry.</summary>
    public async Task<bool> AddAsync(ConfluenceSetupRequest request, bool readOnly, CancellationToken token)
    {
        if (readOnly) { throw new PageMutationRejectedException(UiStrings.PagesReadOnly); }
        ConfluencePageUrlAnalysis analysis = ConfluencePageUrlAnalyzer.Analyze(request.PageUrl);
        ConfluenceConfigSnapshot? snapshot;
        try { snapshot = await store.ReadAsync(token); }
        catch (FileNotFoundException) { snapshot = null; }
        catch (DirectoryNotFoundException) { snapshot = null; }
        ConfluenceConfiguration candidate;
        string key = analysis.InferredSpaceKey ?? request.SpaceKey.Trim();
        if (!ConfluenceSetupService.SpaceKeyPattern().IsMatch(key))
        {
            throw new ConfluenceSetupValidationException(UiStrings.FlowNeedSpace);
        }
        if (snapshot is null)
        {
            candidate = await setup.BuildConfigurationAsync(request with { SpaceKey = key }, token);
        }
        else
        {
            candidate = snapshot.Configuration.MigrateToVersionTwo();
            if (!string.Equals(candidate.BaseUrl?.TrimEnd('/'), analysis.BaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                throw new PageMutationRejectedException(UiStrings.PagesRejectSpaceForeignBaseUrl);
            }
            if (request.AuthExpiresAt != default)
            {
                if (request.AuthExpiresAt <= DateTimeOffset.UtcNow)
                {
                    throw new ConfluenceSetupValidationException(UiStrings.ConfluenceSetupExpiredAuthentication);
                }
                candidate = candidate with { AuthExpiresAt = request.AuthExpiresAt };
            }
            if (!candidate.Spaces.Any(space => string.Equals(space.SpaceKey, key, StringComparison.OrdinalIgnoreCase)))
            {
                if (request.Classification is not ("pro-confidentiel" or "perso-non-sensible"))
                {
                    throw new PageMutationRejectedException(UiStrings.ConfluenceSetupInvalidClassification);
                }
                candidate = candidate.AddSpace(new(key, $"{ConfluenceSetupService.TargetRoot}/{key}",
                    request.Classification, ConfluenceSelection.Pages, []));
            }
        }

        // The temporary configuration carries no secret, so it can outlive the preview: the
        // merge review reads the page tree through the same isolated client.
        string temporary = Path.Combine(Path.GetTempPath(), $"CortexCompanion-preview-{Guid.NewGuid():N}.toml");
        LastSaveMerged = false;
        try
        {
            await using (FileStream stream = new(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await stream.WriteAsync(ConfluenceConfigRenderer.Render(candidate), token);
            }
            IConfluenceCliClient client = previewClient(temporary);
            ConfluenceCliResult<ScopePreviewContract> response = await client.PreviewAsync(request.PageUrl, token);
            if (response.TimedOut)
            {
                throw new PageMutationRejectedException(
                    UiStrings.FormatPagesCliTimedOut(AppConstants.MaximumCliTimeoutSeconds));
            }
            if (response.LaunchError is not null) { throw new PageMutationRejectedException(UiStrings.PagesCliLaunchFailed); }
            if (!response.IsSuccess || response.Value is null)
            {
                throw new ConfluenceCliOperationException(
                    response.ExitCode,
                    response.StandardError,
                    response.TimedOut);
            }
            ScopePreviewContract preview = response.Value;
            if (!string.Equals(preview.SpaceKey, key, StringComparison.OrdinalIgnoreCase))
            {
                throw new PageMutationRejectedException(UiStrings.ConfluenceSetupSpaceMismatch);
            }
            ConfluenceSpaceConfiguration existing = candidate.Spaces.Single(space =>
                string.Equals(space.SpaceKey, key, StringComparison.OrdinalIgnoreCase));
            ConfluenceSelection? selection = confirmations.ChooseScope(preview, preview.IsCovered);
            if (selection is null) { return false; }
            ConfluenceSpaceConfiguration? selected = preview.IsCovered
                ? await MergeAsync(existing, preview, selection.Value, client, token)
                : Extend(existing, preview, selection.Value);
            if (selected is null) { return false; }
            await store.WriteAsync(candidate.MigrateToSchema(selected.Selection == ConfluenceSelection.Subtree ? 3 : 2)
                .ReplaceSpace(selected), snapshot?.ContentHash, token);
            LastSaveRequestsUpdate = confirmations.SaveRequestsUpdate;
            return true;
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    /// <summary>Adds an uncovered page to the source, keeping one collection mode per space.</summary>
    private static ConfluenceSpaceConfiguration Extend(
        ConfluenceSpaceConfiguration existing, ScopePreviewContract preview, ConfluenceSelection selection)
    {
        if (existing.PageIds.Count > 0 && selection != existing.Selection && selection != ConfluenceSelection.WholeSpace)
        {
            throw new PageMutationRejectedException(UiStrings.FlowExistingScope);
        }
        return existing with
        {
            Selection = selection,
            PageIds = selection == ConfluenceSelection.WholeSpace ? [] :
                existing.PageIds.Append(preview.PageId).Distinct(StringComparer.Ordinal).ToArray(),
        };
    }

    /// <summary>Offers to widen the source or to replace its selection, and returns the chosen source.</summary>
    /// <remarks>
    /// The page tree names the documents each merge adds or removes. Without it the review
    /// still opens and says that it could not measure them, as the editor does.
    /// </remarks>
    private async Task<ConfluenceSpaceConfiguration?> MergeAsync(
        ConfluenceSpaceConfiguration existing,
        ScopePreviewContract preview,
        ConfluenceSelection selection,
        IConfluenceCliClient client,
        CancellationToken token)
    {
        ConfluenceSpaceConfiguration? widen = SourceMergeReview.WidenCandidate(existing, selection, preview.PageId);
        ConfluenceSpaceConfiguration? replace = SourceMergeReview.ReplaceCandidate(existing, selection, preview.PageId);
        if (widen is null && replace is null)
        {
            throw new PageMutationRejectedException(UiStrings.PagesRejectPageAlreadyConfigured);
        }
        ConfluenceCliResult<SourceCatalogContract> read = await client.GetCatalogAsync(existing.SpaceKey, token);
        SourceCatalogContract? catalog = read.IsSuccess && read.Value?.SpaceKey == existing.SpaceKey ? read.Value : null;
        ConfiguredPageContract[] known = [new() { PageId = preview.PageId, Title = preview.Title }];
        SourceMergeReview review = new(
            preview,
            existing,
            widen is null ? null : SourceChangeReview.Create(existing, widen, catalog, known),
            replace is null ? null : SourceChangeReview.Create(existing, replace, catalog, known))
        {
            CoveringRootTitle = preview.CoveringRoot is null
                ? null
                : catalog?.Pages.FirstOrDefault(page => page.PageId == preview.CoveringRoot)?.Title,
        };
        SourceMergeChoice? choice = confirmations.ChooseMerge(review);
        if (choice is null) { return null; }
        ConfluenceSpaceConfiguration chosen = (choice == SourceMergeChoice.Widen ? widen : replace)
            ?? throw new PageMutationRejectedException(UiStrings.PagesRejectPageAlreadyConfigured);
        LastSaveMerged = true;
        return chosen;
    }
}
