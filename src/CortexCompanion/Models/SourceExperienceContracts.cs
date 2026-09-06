// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text.Json.Serialization;

namespace CortexCompanion.Models;

/// <summary>Represents a remotely verified page and its ordered ancestor identifiers.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CatalogPageContract
{
    /// <summary>Gets the stable numeric page identifier.</summary>
    [JsonPropertyName("page_id")]
    public required string PageId { get; init; }
    /// <summary>Gets the verified page title.</summary>
    [JsonPropertyName("title")]
    public required string Title { get; init; }
    /// <summary>Gets the ordered remote ancestor chain.</summary>
    [JsonPropertyName("ancestor_ids")]
    public required IReadOnlyList<string> AncestorIds { get; init; }
}

/// <summary>Contains a complete allowlisted space catalogue; partial output is rejected.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SourceCatalogContract
{
    /// <summary>Gets the independently versioned machine contract.</summary>
    [JsonPropertyName("contract_version")]
    public required int ContractVersion { get; init; }
    /// <summary>Gets the allowlisted space identity.</summary>
    [JsonPropertyName("space_key")]
    public required string SpaceKey { get; init; }
    /// <summary>Gets the complete verified page catalogue.</summary>
    [JsonPropertyName("pages")]
    public required IReadOnlyList<CatalogPageContract> Pages { get; init; }
}

/// <summary>Contains local publication evidence, independent of search indexing.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SourceStatusContract
{
    /// <summary>Gets the independently versioned machine contract.</summary>
    [JsonPropertyName("contract_version")]
    public required int ContractVersion { get; init; }
    /// <summary>Reports whether current configuration matches the last published selection.</summary>
    [JsonPropertyName("selection_current")]
    public required bool SelectionCurrent { get; init; }
    /// <summary>Identifies the current published generation; this is not indexing proof.</summary>
    [JsonPropertyName("generation_id")]
    public string? GenerationId { get; init; }
    /// <summary>Gets the most recent observed source health status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; init; }
}

/// <summary>Shows identities affected by a scope change, with explicit measurement coverage.</summary>
public sealed record SourceChangeReview(
    ConfluenceSpaceConfiguration Before, ConfluenceSpaceConfiguration After,
    IReadOnlyList<CatalogPageContract> Added, IReadOnlyList<CatalogPageContract> Removed, bool IsComplete)
{
    /// <summary>Gets newly selected roots independently of effective document changes.</summary>
    public IReadOnlyList<CatalogPageContract> AddedRoots { get; init; } = [];
    /// <summary>Gets removed roots even if another subtree continues covering their documents.</summary>
    public IReadOnlyList<CatalogPageContract> RemovedRoots { get; init; } = [];

    /// <summary>Calculates the effective document difference from verified ancestor chains.</summary>
    public static SourceChangeReview Create(ConfluenceSpaceConfiguration before, ConfluenceSpaceConfiguration after,
        SourceCatalogContract? catalog, IReadOnlyList<ConfiguredPageContract> knownPages)
    {
        bool complete = catalog is not null && before.PageIds.Concat(after.PageIds)
            .All(id => catalog.Pages.Any(page => page.PageId == id));
        IReadOnlyList<CatalogPageContract> pages = catalog?.Pages ?? before.PageIds.Concat(after.PageIds)
            .Distinct(StringComparer.Ordinal).Select(id => new CatalogPageContract
            {
                PageId = id,
                Title = knownPages.FirstOrDefault(page => page.PageId == id)?.Title ?? id,
                AncestorIds = [],
            }).ToArray();
        HashSet<string> previous = Included(before, pages);
        HashSet<string> next = Included(after, pages);
        return new SourceChangeReview(before, after,
            pages.Where(page => next.Contains(page.PageId) && !previous.Contains(page.PageId)).ToArray(),
            pages.Where(page => previous.Contains(page.PageId) && !next.Contains(page.PageId)).ToArray(), complete)
        {
            AddedRoots = RootDifference(after.PageIds.Except(before.PageIds), pages, knownPages),
            RemovedRoots = RootDifference(before.PageIds.Except(after.PageIds), pages, knownPages),
        };
    }

    private static CatalogPageContract[] RootDifference(IEnumerable<string> ids,
        IReadOnlyList<CatalogPageContract> catalog, IReadOnlyList<ConfiguredPageContract> knownPages) => ids.Select(id =>
            catalog.FirstOrDefault(page => page.PageId == id) ?? new CatalogPageContract
            { PageId = id, Title = knownPages.FirstOrDefault(page => page.PageId == id)?.Title ?? id, AncestorIds = [] }).ToArray();

    private static HashSet<string> Included(ConfluenceSpaceConfiguration space, IReadOnlyList<CatalogPageContract> pages) =>
        pages.Where(page => space.Selection == ConfluenceSelection.WholeSpace || space.PageIds.Contains(page.PageId) ||
            (space.Selection == ConfluenceSelection.Subtree && page.AncestorIds.Any(space.PageIds.Contains)))
            .Select(page => page.PageId).ToHashSet(StringComparer.Ordinal);
}
