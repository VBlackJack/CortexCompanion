// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Models;

/// <summary>Names the two ways a page can join the source that already collects it.</summary>
public enum SourceMergeChoice
{
    /// <summary>Keeps everything the source collects and adds the chosen scope to it.</summary>
    Widen,

    /// <summary>Keeps only the chosen scope.</summary>
    Replace,
}

/// <summary>Shows what widening the source and replacing its selection would each do.</summary>
/// <remarks>
/// A candidate that leaves the source as it is has no review: offering it would ask the user
/// to confirm a write that changes nothing. Both can be absent, and the caller refuses then.
/// </remarks>
public sealed record SourceMergeReview(
    ScopePreviewContract Preview,
    ConfluenceSpaceConfiguration Existing,
    SourceChangeReview? Widen,
    SourceChangeReview? Replace)
{
    /// <summary>Gets the title of the listed page that covers the previewed one, when known.</summary>
    public string? CoveringRootTitle { get; init; }

    /// <summary>Builds the source as widened by the chosen scope, or null when nothing changes.</summary>
    /// <remarks>
    /// The schema has no mixed mode, so a source tracking pages alone that widens to a subtree
    /// makes every listed page a root; the review shows what that adds. A source already
    /// collecting the whole space has nothing to widen to.
    /// </remarks>
    public static ConfluenceSpaceConfiguration? WidenCandidate(
        ConfluenceSpaceConfiguration existing,
        ConfluenceSelection selection,
        string pageId)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ConfluenceSelection widened = Rank(selection) > Rank(existing.Selection) ? selection : existing.Selection;
        ConfluenceSpaceConfiguration candidate = existing with
        {
            Selection = widened,
            PageIds = widened == ConfluenceSelection.WholeSpace
                ? []
                : existing.PageIds.Append(pageId).Distinct(StringComparer.Ordinal).ToArray(),
        };
        return candidate.SemanticallyEquals(existing) ? null : candidate;
    }

    /// <summary>Builds the source as replaced by the chosen scope alone, or null when nothing changes.</summary>
    public static ConfluenceSpaceConfiguration? ReplaceCandidate(
        ConfluenceSpaceConfiguration existing,
        ConfluenceSelection selection,
        string pageId)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ConfluenceSpaceConfiguration candidate = existing with
        {
            Selection = selection,
            PageIds = selection == ConfluenceSelection.WholeSpace ? [] : [pageId],
        };
        return candidate.SemanticallyEquals(existing) ? null : candidate;
    }

    private static int Rank(ConfluenceSelection selection) => selection switch
    {
        ConfluenceSelection.WholeSpace => 2,
        ConfluenceSelection.Subtree => 1,
        _ => 0,
    };
}
