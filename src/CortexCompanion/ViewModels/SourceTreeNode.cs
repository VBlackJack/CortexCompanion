// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Collections.ObjectModel;
using CortexCompanion.Models;

namespace CortexCompanion.ViewModels;

/// <summary>A selectable page whose tree position comes from verified remote ancestors.</summary>
public sealed class SourceTreeNode : ViewModelBase
{
    private bool _isSelected;
    private bool _isVisible = true;
    private bool _isCovered;
    /// <summary>Creates one page without inferring missing hierarchy.</summary>
    public SourceTreeNode(string pageId, string title, IReadOnlyList<string> ancestorIds, bool selected)
    { PageId = pageId; Title = title; AncestorIds = ancestorIds; _isSelected = selected; }
    /// <summary>Gets the remotely verified stable page identifier.</summary>
    public string PageId { get; }
    /// <summary>Gets the visible remote title.</summary>
    public string Title { get; }
    /// <summary>Gets the verified ancestor chain in root-to-parent order.</summary>
    public IReadOnlyList<string> AncestorIds { get; }
    /// <summary>Gets the observed child nodes; missing parents are not invented.</summary>
    public ObservableCollection<SourceTreeNode> Children { get; } = [];
    /// <summary>Indicates explicit root selection independently of inherited inclusion.</summary>
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    /// <summary>Controls search filtering without changing selection.</summary>
    public bool IsVisible { get => _isVisible; set => SetProperty(ref _isVisible, value); }
    /// <summary>Indicates implicit inclusion through another selected ancestor.</summary>
    public bool IsCovered { get => _isCovered; set => SetProperty(ref _isCovered, value); }

    /// <summary>Builds only acyclic relationships, preserving selected pages absent from the catalogue.</summary>
    public static IReadOnlyList<SourceTreeNode> Build(IReadOnlyList<CatalogPageContract> pages,
        IReadOnlyList<SourceTreeNode> existing)
    {
        Dictionary<string, SourceTreeNode> nodes = pages.ToDictionary(page => page.PageId,
            page => new SourceTreeNode(page.PageId, page.Title, page.AncestorIds,
                existing.Any(item => item.PageId == page.PageId && item.IsSelected)), StringComparer.Ordinal);
        foreach (SourceTreeNode old in existing.Where(item => !nodes.ContainsKey(item.PageId)))
        { nodes.Add(old.PageId, new SourceTreeNode(old.PageId, old.Title, [], old.IsSelected)); }
        List<SourceTreeNode> roots = [];
        foreach (SourceTreeNode node in nodes.Values)
        {
            string? parentId = node.AncestorIds.LastOrDefault(nodes.ContainsKey);
            HashSet<string> chain = new(StringComparer.Ordinal) { node.PageId };
            string? cursor = parentId;
            bool cycle = false;
            while (cursor is not null)
            {
                if (!chain.Add(cursor)) { cycle = true; break; }
                cursor = nodes[cursor].AncestorIds.LastOrDefault(nodes.ContainsKey);
            }
            if (parentId is not null && !cycle)
            { nodes[parentId].Children.Add(node); }
            else { roots.Add(node); }
        }
        return roots;
    }

    /// <summary>Enumerates a validated tree iteratively to avoid recursion on deep spaces.</summary>
    public static IEnumerable<SourceTreeNode> Flatten(IEnumerable<SourceTreeNode> roots)
    {
        Stack<SourceTreeNode> pending = new(roots.Reverse());
        HashSet<string> visited = new(StringComparer.Ordinal);
        while (pending.TryPop(out SourceTreeNode? node))
        {
            if (!visited.Add(node.PageId)) { continue; }
            yield return node;
            foreach (SourceTreeNode child in node.Children.Reverse()) { pending.Push(child); }
        }
    }

    /// <summary>Keeps matching pages and their ancestors visible without altering selection.</summary>
    public static void Filter(IEnumerable<SourceTreeNode> roots, string query)
    {
        SourceTreeNode[] all = Flatten(roots).ToArray();
        HashSet<string> matching = all.Where(node => node.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            node.PageId.Contains(query, StringComparison.OrdinalIgnoreCase)).Select(node => node.PageId).ToHashSet(StringComparer.Ordinal);
        foreach (SourceTreeNode node in all.Where(node => matching.Contains(node.PageId)))
        { foreach (string ancestor in node.AncestorIds) { matching.Add(ancestor); } }
        foreach (SourceTreeNode node in all) { node.IsVisible = matching.Contains(node.PageId); }
    }
}
