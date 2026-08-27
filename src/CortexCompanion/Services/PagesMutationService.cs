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

    /// <summary>Resolves, presents, and then persists one page ID without ever persisting its title.</summary>
    public async Task<bool> AddPageAsync(
        string reference,
        bool isReadOnly,
        CancellationToken cancellationToken)
    {
        EnsureMutable(isReadOnly);
        ConfluenceCliResult<ResolvedPageContract> resolution = await _cliClient.ResolveAsync(
            reference,
            cancellationToken);
        if (!resolution.IsSuccess || resolution.Value is null)
        {
            throw new ConfluenceCliOperationException(resolution.ExitCode, resolution.StandardError);
        }

        ConfluenceConfigSnapshot snapshot = await _configStore.ReadAsync(cancellationToken);
        ConfluenceSpaceConfiguration space = FindSpace(snapshot.Configuration, resolution.Value.SpaceKey);
        if (space.Selection == ConfluenceSelection.WholeSpace)
        {
            throw new PageMutationRejectedException(UiStrings.PagesRejectWholeSpaceCovered);
        }

        if (space.PageIds.Contains(resolution.Value.PageId, StringComparer.Ordinal))
        {
            throw new PageMutationRejectedException(UiStrings.PagesRejectPageAlreadyConfigured);
        }

        if (!_confirmations.ConfirmAdd(resolution.Value))
        {
            return false;
        }

        ConfluenceSpaceConfiguration replacement = space with
        {
            PageIds = space.PageIds.Append(resolution.Value.PageId).ToArray(),
        };
        await WriteOrRefreshAsync(
            snapshot.Configuration.ReplaceSpace(replacement),
            snapshot.ContentHash,
            cancellationToken);
        return true;
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
        if (space.Selection != ConfluenceSelection.Pages ||
            !space.PageIds.Contains(pageId, StringComparer.Ordinal))
        {
            throw new PageMutationRejectedException(UiStrings.PagesRejectPageNotConfigured);
        }

        if (!_confirmations.ConfirmRemove(spaceKey, pageId, title))
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
        ConfluenceConfiguration migrated = snapshot.Configuration.MigrateToVersionTwo();
        ConfluenceSpaceConfiguration current = FindSpace(migrated, spaceKey);
        ConfluenceSelection target = current.Selection == ConfluenceSelection.WholeSpace
            ? ConfluenceSelection.Pages
            : ConfluenceSelection.WholeSpace;
        IReadOnlyList<string> targetPages = target == ConfluenceSelection.Pages
            ? Array.Empty<string>()
            : current.PageIds;
        string? typed = _confirmations.ConfirmModeChange(spaceKey, target, targetPages);
        if (!string.Equals(typed, spaceKey, StringComparison.Ordinal))
        {
            return false;
        }

        ConfluenceSpaceConfiguration replacement = current with
        {
            Selection = target,
            PageIds = Array.Empty<string>(),
        };
        await WriteOrRefreshAsync(
            migrated.ReplaceSpace(replacement),
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
