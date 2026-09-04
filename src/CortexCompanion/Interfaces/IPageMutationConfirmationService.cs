// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;

namespace CortexCompanion.Interfaces;

/// <summary>Abstracts the explicit confirmations required before every Pages mutation.</summary>
public interface IPageMutationConfirmationService
{
    /// <summary>Confirms a resolved page identity before its numeric ID can be persisted.</summary>
    bool ConfirmAdd(ResolvedPageContract page);

    /// <summary>Returns the explicit measured collection choice, or null when cancelled.</summary>
    ConfluenceSelection? ChooseScope(ScopePreviewContract preview);

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
