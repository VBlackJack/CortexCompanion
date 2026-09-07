// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;

namespace CortexCompanion.Interfaces;

/// <summary>Abstracts the explicit confirmations required before every Pages mutation.</summary>
public interface IPageMutationConfirmationService
{
    /// <summary>Gets the explicit save-and-update choice made in a scope dialog.</summary>
    bool SaveRequestsUpdate => false;

    /// <summary>Provides an optional remote catalogue loader without changing legacy confirmations.</summary>
    SourceSelectionEdit? EditSelectionWithCatalog(ConfluenceSpaceConfiguration space, IReadOnlyList<ConfiguredPageContract> pages,
        Func<Task<ConfluenceCliResult<SourceCatalogContract>>> loadCatalog) => EditSelection(space, pages);

    /// <summary>Confirms the measured effective document difference.</summary>
    bool ConfirmSelectionReview(SourceChangeReview review) => ConfirmSelection(review.Before, review.After);

    /// <summary>Edits a previously loaded selection; dismissal never authorizes a write.</summary>
    SourceSelectionEdit? EditSelection(ConfluenceSpaceConfiguration space, IReadOnlyList<ConfiguredPageContract> pages) => null;

    /// <summary>Confirms the exact replacement after validation.</summary>
    bool ConfirmSelection(ConfluenceSpaceConfiguration before, ConfluenceSpaceConfiguration after) => false;

    /// <summary>Confirms removal of an entire configured source.</summary>
    bool ConfirmRemoveSource(string spaceKey) => false;

    /// <summary>Confirms a page removal with a best-effort check of remaining root coverage.</summary>
    bool ConfirmRemoveWithCoverage(string spaceKey, string pageId, string? title, bool? stillCovered) =>
        ConfirmRemove(spaceKey, pageId, title);

    /// <summary>Confirms a resolved page identity before its numeric ID can be persisted.</summary>
    bool ConfirmAdd(ResolvedPageContract page);

    /// <summary>Returns the explicit measured collection choice, or null when cancelled.</summary>
    ConfluenceSelection? ChooseScope(ScopePreviewContract preview);

    /// <summary>Returns the same choice, told whether the page is already tracked.</summary>
    /// <remarks>
    /// Whether an already tracked page can be added depends on the scope the user picks, so
    /// the answer cannot be settled before the question. Saying so in the window is what
    /// stops the user from choosing carefully and only then being refused.
    /// </remarks>
    ConfluenceSelection? ChooseScope(ScopePreviewContract preview, bool alreadyTracked) =>
        ChooseScope(preview);

    /// <summary>Confirms the collection consequence before a space enters the allowlist.</summary>
    bool ConfirmAddSpace(string spaceKey, string classification);

    /// <summary>Asks whether a space that would collect nothing should stay allowlisted.</summary>
    bool ConfirmKeepEmptySpace(string spaceKey);

    /// <summary>Confirms the tombstone consequence before removing a configured page.</summary>
    bool ConfirmRemove(string spaceKey, string pageId, string? title);

    /// <summary>Collects the typed space-key confirmation for an exact collection-mode consequence.</summary>
    string? ConfirmModeChange(
        string spaceKey,
        ConfluenceSelection targetSelection,
        IReadOnlyList<string> targetPageIds);
}
