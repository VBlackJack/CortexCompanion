// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Coordinates resolve-first, confirmed, exact-byte CAS mutations for the Pages screen.</summary>
public sealed class PagesMutationService
{
    private readonly IConfluenceCliClient _cliClient;
    private readonly IConfluenceConfigStore _configStore;
    private readonly IPageMutationConfirmationService _confirmations;

    /// <summary>Initializes the mutation workflow with mockable process, storage, and confirmation boundaries.</summary>
    public PagesMutationService(
        IConfluenceCliClient cliClient,
        IConfluenceConfigStore configStore,
        IPageMutationConfirmationService confirmations)
    {
        _cliClient = cliClient ?? throw new ArgumentNullException(nameof(cliClient));
        _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
        _confirmations = confirmations ?? throw new ArgumentNullException(nameof(confirmations));
    }

    /// <summary>Measures, presents, and persists one explicit scope without storing the title.</summary>
    public async Task<bool> AddPageAsync(
        string reference,
        bool isReadOnly,
        CancellationToken cancellationToken)
    {
        EnsureMutable(isReadOnly);
        ConfluenceCliResult<ScopePreviewContract> preview = await _cliClient.PreviewAsync(
            reference,
            cancellationToken);
        if (!preview.IsSuccess || preview.Value is null)
        {
            throw new ConfluenceCliOperationException(preview.ExitCode, preview.StandardError);
        }

        ConfluenceConfigSnapshot snapshot = await _configStore.ReadAsync(cancellationToken);
        ConfluenceSpaceConfiguration space = FindSpace(snapshot.Configuration, preview.Value.SpaceKey);
        if (space.Selection == ConfluenceSelection.WholeSpace)
        {
            throw new PageMutationRejectedException(UiStrings.PagesRejectWholeSpaceCovered);
        }

        if (space.PageIds.Contains(preview.Value.PageId, StringComparer.Ordinal))
        {
            throw new PageMutationRejectedException(UiStrings.PagesRejectPageAlreadyConfigured);
        }

        ConfluenceSelection? selectedScope = _confirmations.ChooseScope(preview.Value);
        if (selectedScope is null)
        {
            return false;
        }

        int targetSchema = selectedScope == ConfluenceSelection.Subtree
            ? ConfluenceConfigParser.SubtreeSchemaVersion
            : 2;
        ConfluenceConfiguration migrated = snapshot.Configuration.MigrateToSchema(targetSchema);
        space = FindSpace(migrated, preview.Value.SpaceKey);
        IReadOnlyList<string> pageIds = selectedScope == ConfluenceSelection.WholeSpace
            ? Array.Empty<string>()
            : space.PageIds.Append(preview.Value.PageId).ToArray();
        ConfluenceSpaceConfiguration replacement = space with
        {
            Selection = selectedScope.Value,
            PageIds = pageIds,
        };
        await WriteOrRefreshAsync(
            migrated.ReplaceSpace(replacement),
            snapshot.ContentHash,
            cancellationToken);
        return true;
    }

    /// <summary>Allowlists the space a page URL points at, so its pages become configurable.</summary>
    public async Task<bool> AddSpaceAsync(
        string reference,
        string classification,
        bool isReadOnly,
        CancellationToken cancellationToken)
    {
        EnsureMutable(isReadOnly);
        if (classification is not "pro-confidentiel" and not "perso-non-sensible")
        {
            throw new PageMutationRejectedException(UiStrings.ConfluenceSetupInvalidClassification);
        }

        ConfluencePageUrlAnalysis analysis = ConfluencePageUrlAnalyzer.Analyze(reference);
        if (analysis.InferredSpaceKey is null)
        {
            throw new PageMutationRejectedException(UiStrings.PagesRejectSpaceKeyNotInferable);
        }

        string spaceKey = analysis.InferredSpaceKey;
        if (!ConfluenceSetupService.SpaceKeyPattern().IsMatch(spaceKey))
        {
            throw new PageMutationRejectedException(UiStrings.ConfluenceSetupInvalidSpaceKey);
        }

        ConfluenceConfigSnapshot snapshot = await _configStore.ReadAsync(cancellationToken);

        // A URL from another Confluence server would allowlist a space this configuration
        // can never reach, so the origin is checked before anything is written.
        if (snapshot.Configuration.BaseUrl is not null &&
            !string.Equals(snapshot.Configuration.BaseUrl, analysis.BaseUrl, StringComparison.OrdinalIgnoreCase))
        {
            throw new PageMutationRejectedException(UiStrings.PagesRejectSpaceForeignBaseUrl);
        }

        if (snapshot.Configuration.Spaces.Any(space =>
            string.Equals(space.SpaceKey, spaceKey, StringComparison.OrdinalIgnoreCase)))
        {
            throw new PageMutationRejectedException(UiStrings.PagesRejectSpaceAlreadyAllowlisted);
        }

        if (!_confirmations.ConfirmAddSpace(spaceKey, classification))
        {
            return false;
        }

        // The space enters empty in explicit-pages mode: allowlisting authorizes nothing
        // on its own, and the caller adds the page that motivated it right after.
        ConfluenceSpaceConfiguration created = new(
            spaceKey,
            $"{ConfluenceSetupService.TargetRoot}/{spaceKey}",
            classification,
            ConfluenceSelection.Pages,
            Array.Empty<string>());
        await WriteOrRefreshAsync(
            snapshot.Configuration.MigrateToVersionTwo().AddSpace(created),
            snapshot.ContentHash,
            cancellationToken);

        // Allowlisting is a step, not the goal. If the page this space came from is not
        // added, the space would collect nothing and nothing downstream would say so:
        // the CLI logs no line for a selection it never enumerates, and the health stays
        // ok because a space with no page has no document to fail over.
        bool pageAdded;
        try
        {
            pageAdded = await AddPageAsync(reference, isReadOnly, cancellationToken);
        }
        catch
        {
            await ConfirmOrDiscardEmptySpaceAsync(spaceKey, cancellationToken);
            throw;
        }

        if (!pageAdded)
        {
            await ConfirmOrDiscardEmptySpaceAsync(spaceKey, cancellationToken);
        }

        return pageAdded;
    }

    /// <summary>Asks whether a space that collects nothing should stay, and removes it otherwise.</summary>
    public async Task ConfirmOrDiscardEmptySpaceAsync(string spaceKey, CancellationToken cancellationToken)
    {
        if (_confirmations.ConfirmKeepEmptySpace(spaceKey))
        {
            return;
        }

        ConfluenceConfigSnapshot snapshot = await _configStore.ReadAsync(cancellationToken);
        if (!snapshot.Configuration.Spaces.Any(space =>
            string.Equals(space.SpaceKey, spaceKey, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        await WriteOrRefreshAsync(
            snapshot.Configuration.RemoveSpace(spaceKey),
            snapshot.ContentHash,
            cancellationToken);
    }

    /// <summary>Removes one configured ID only after the explicit tombstone reminder is accepted.</summary>
    public async Task<bool> RemovePageAsync(
        string spaceKey,
        string pageId,
        string? title,
        bool isReadOnly,
        CancellationToken cancellationToken)
    {
        EnsureMutable(isReadOnly);
        ConfluenceConfigSnapshot snapshot = await _configStore.ReadAsync(cancellationToken);
        ConfluenceSpaceConfiguration space = FindSpace(snapshot.Configuration, spaceKey);
        if (space.Selection == ConfluenceSelection.WholeSpace ||
            !space.PageIds.Contains(pageId, StringComparer.Ordinal))
        {
            throw new PageMutationRejectedException(UiStrings.PagesRejectPageNotConfigured);
        }

        if (!_confirmations.ConfirmRemove(spaceKey, pageId, title))
        {
            return false;
        }

        if (space.PageIds.Count == 1 && !_confirmations.ConfirmKeepEmptySpace(spaceKey))
        {
            return false;
        }

        ConfluenceSpaceConfiguration replacement = space with
        {
            PageIds = space.PageIds.Where(candidate => candidate != pageId).ToArray(),
        };
        await WriteOrRefreshAsync(
            snapshot.Configuration.ReplaceSpace(replacement),
            snapshot.ContentHash,
            cancellationToken);
        return true;
    }

    /// <summary>Switches collection mode only when the user types the exact space key.</summary>
    public async Task<bool> SwitchModeAsync(
        string spaceKey,
        bool isReadOnly,
        CancellationToken cancellationToken)
    {
        EnsureMutable(isReadOnly);
        ConfluenceConfigSnapshot snapshot = await _configStore.ReadAsync(cancellationToken);
        ConfluenceSelection current = FindSpace(
            snapshot.Configuration.MigrateToVersionTwo(),
            spaceKey).Selection;
        ConfluenceSelection target = current switch
        {
            ConfluenceSelection.WholeSpace => ConfluenceSelection.Pages,
            ConfluenceSelection.Pages => ConfluenceSelection.Subtree,
            _ => ConfluenceSelection.WholeSpace,
        };
        ConfluenceConfiguration migrated = snapshot.Configuration.MigrateToSchema(
            target == ConfluenceSelection.Subtree ? ConfluenceConfigParser.SubtreeSchemaVersion : 2);
        ConfluenceSpaceConfiguration space = FindSpace(migrated, spaceKey);

        // Explicit identifiers survive the pages-to-subtree step: they become the roots
        // whose descendants the next collection adds. Every other step clears the list.
        IReadOnlyList<string> targetPages = target == ConfluenceSelection.Subtree
            ? space.PageIds
            : Array.Empty<string>();
        string? typed = _confirmations.ConfirmModeChange(spaceKey, target, targetPages);
        if (!string.Equals(typed, spaceKey, StringComparison.Ordinal))
        {
            return false;
        }

        ConfluenceSpaceConfiguration replacement = space with
        {
            Selection = target,
            PageIds = targetPages,
        };
        await WriteOrRefreshAsync(
            migrated.ReplaceSpace(replacement),
            snapshot.ContentHash,
            cancellationToken);
        return true;
    }

    /// <summary>Expands an explicit page selection to its subtrees from a precise corrective action.</summary>
    public async Task<bool> ExpandToSubtreeAsync(
        string spaceKey,
        bool isReadOnly,
        CancellationToken cancellationToken)
    {
        EnsureMutable(isReadOnly);
        ConfluenceConfigSnapshot snapshot = await _configStore.ReadAsync(cancellationToken);
        ConfluenceConfiguration migrated = snapshot.Configuration.MigrateToSchema(
            ConfluenceConfigParser.SubtreeSchemaVersion);
        ConfluenceSpaceConfiguration space = FindSpace(migrated, spaceKey);
        if (space.Selection != ConfluenceSelection.Pages || space.PageIds.Count == 0)
        {
            throw new PageMutationRejectedException(UiStrings.PagesRejectPageNotConfigured);
        }

        await WriteOrRefreshAsync(
            migrated.ReplaceSpace(space with { Selection = ConfluenceSelection.Subtree }),
            snapshot.ContentHash,
            cancellationToken);
        return true;
    }

    private async Task WriteOrRefreshAsync(
        ConfluenceConfiguration configuration,
        string expectedHash,
        CancellationToken cancellationToken)
    {
        try
        {
            await _configStore.WriteAsync(configuration, expectedHash, cancellationToken);
        }
        catch (ConfluenceConfigConflictException exception)
        {
            ConfluenceConfigSnapshot current = await _configStore.ReadAsync(cancellationToken);
            throw new ConfluenceConfigRefreshRequiredException(exception.Message, current, exception);
        }
    }

    private static ConfluenceSpaceConfiguration FindSpace(
        ConfluenceConfiguration configuration,
        string spaceKey) =>
        configuration.Spaces.SingleOrDefault(space =>
            string.Equals(space.SpaceKey, spaceKey, StringComparison.OrdinalIgnoreCase))
        ?? throw new PageMutationRejectedException(UiStrings.PagesRejectSpaceNotAllowlisted);

    private static void EnsureMutable(bool isReadOnly)
    {
        if (isReadOnly)
        {
            throw new PageMutationRejectedException(UiStrings.PagesRejectReadOnly);
        }
    }
}

/// <summary>Reports a user-safe CLI result to the Pages view model.</summary>
public sealed class ConfluenceCliOperationException : Exception
{
    /// <summary>Initializes an operation error with its stable code and sanitized message.</summary>
    public ConfluenceCliOperationException(CortexExitCode exitCode, string message)
        : base(message)
    {
        ExitCode = exitCode;
    }

    /// <summary>Gets the frozen Cortex exit code.</summary>
    public CortexExitCode ExitCode { get; }
}

/// <summary>Reports a business-rule refusal that requires no write.</summary>
public sealed class PageMutationRejectedException : Exception
{
    /// <summary>Initializes a user-safe refusal.</summary>
    public PageMutationRejectedException(string message)
        : base(message)
    {
    }
}

/// <summary>Reports a CAS refusal together with the freshly reloaded raw snapshot.</summary>
public sealed class ConfluenceConfigRefreshRequiredException : Exception
{
    /// <summary>Initializes a refresh requirement.</summary>
    public ConfluenceConfigRefreshRequiredException(
        string message,
        ConfluenceConfigSnapshot currentSnapshot,
        Exception innerException)
        : base(message, innerException)
    {
        CurrentSnapshot = currentSnapshot;
    }

    /// <summary>Gets the reloaded current snapshot that invalidated the prior presentation.</summary>
    public ConfluenceConfigSnapshot CurrentSnapshot { get; }
}
