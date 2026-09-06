// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

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

        string temporary = Path.Combine(Path.GetTempPath(), $"CortexCompanion-preview-{Guid.NewGuid():N}.toml");
        ScopePreviewContract preview;
        try
        {
            await using (FileStream stream = new(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await stream.WriteAsync(ConfluenceConfigRenderer.Render(candidate), token);
            }
            ConfluenceCliResult<ScopePreviewContract> response = await previewClient(temporary).PreviewAsync(request.PageUrl, token);
            if (response.TimedOut) { throw new PageMutationRejectedException(UiStrings.PagesCliTimedOut); }
            if (response.LaunchError is not null) { throw new PageMutationRejectedException(UiStrings.PagesCliLaunchFailed); }
            if (!response.IsSuccess || response.Value is null)
            {
                throw new ConfluenceCliOperationException(response.ExitCode, response.StandardError);
            }
            preview = response.Value;
        }
        finally
        {
            File.Delete(temporary);
        }

        if (!string.Equals(preview.SpaceKey, key, StringComparison.OrdinalIgnoreCase))
        {
            throw new PageMutationRejectedException(UiStrings.ConfluenceSetupSpaceMismatch);
        }
        ConfluenceSpaceConfiguration existing = candidate.Spaces.Single(space =>
            string.Equals(space.SpaceKey, key, StringComparison.OrdinalIgnoreCase));
        if (existing.Selection == ConfluenceSelection.WholeSpace)
        {
            throw new PageMutationRejectedException(UiStrings.PagesRejectWholeSpaceCovered);
        }
        ConfluenceSelection? selection = confirmations.ChooseScope(preview);
        if (selection is null) { return false; }
        if (existing.PageIds.Count > 0 && selection != existing.Selection && selection != ConfluenceSelection.WholeSpace)
        {
            throw new PageMutationRejectedException(UiStrings.FlowExistingScope);
        }
        if (existing.PageIds.Contains(preview.PageId, StringComparer.Ordinal) && selection != ConfluenceSelection.WholeSpace)
        {
            throw new PageMutationRejectedException(UiStrings.PagesRejectPageAlreadyConfigured);
        }
        ConfluenceSpaceConfiguration selected = existing with
        {
            Selection = selection.Value,
            PageIds = selection == ConfluenceSelection.WholeSpace ? [] :
                existing.PageIds.Append(preview.PageId).Distinct(StringComparer.Ordinal).ToArray(),
        };
        await store.WriteAsync(candidate.MigrateToSchema(selection == ConfluenceSelection.Subtree ? 3 : 2)
            .ReplaceSpace(selected), snapshot?.ContentHash, token);
        return true;
    }
}
