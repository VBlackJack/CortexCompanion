// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;
using CortexCompanion.ViewModels;

namespace CortexCompanion.Tests.Models;

[TestClass]
public sealed class SourceExperienceTests
{
    private static CatalogPageContract Page(string id, params string[] ancestors) => new()
    { PageId = id, Title = "Page " + id, AncestorIds = ancestors };
    private static ConfluenceSpaceConfiguration Space(ConfluenceSelection selection, params string[] ids) =>
        new("DOC", "doc", "pro-confidentiel", selection, ids);

    [TestMethod]
    public void RemovingOverlappingRootDoesNotPretendToRemoveItsDocuments()
    {
        SourceCatalogContract catalog = new() { ContractVersion = 1, SpaceKey = "DOC", Pages = [Page("1"), Page("2", "1"), Page("3", "1", "2")] };
        SourceChangeReview review = SourceChangeReview.Create(Space(ConfluenceSelection.Subtree, "1", "2"),
            Space(ConfluenceSelection.Subtree, "1"), catalog, []);
        Assert.IsTrue(review.IsComplete);
        Assert.IsEmpty(review.Removed);
        Assert.IsEmpty(review.Added);
    }

    [TestMethod]
    public void NarrowingWholeSpaceReportsEffectiveRemovedDescendants()
    {
        SourceCatalogContract catalog = new() { ContractVersion = 1, SpaceKey = "DOC", Pages = [Page("1"), Page("2", "1"), Page("3", "1", "2")] };
        SourceChangeReview review = SourceChangeReview.Create(Space(ConfluenceSelection.WholeSpace),
            Space(ConfluenceSelection.Pages, "1"), catalog, []);
        Assert.IsTrue(review.IsComplete);
        Assert.HasCount(2, review.Removed);
        Assert.IsEmpty(review.Added);
    }

    [TestMethod]
    public void MissingCatalogueNeverClaimsMeasuredImpact()
    {
        SourceChangeReview review = SourceChangeReview.Create(Space(ConfluenceSelection.Pages, "1"),
            Space(ConfluenceSelection.Subtree, "1"), null, []);
        Assert.IsFalse(review.IsComplete);
    }

    [TestMethod]
    public void TreeFilterKeepsAncestorsAndSelections()
    {
        IReadOnlyList<SourceTreeNode> tree = SourceTreeNode.Build([Page("1"), Page("2", "1"), Page("3")],
            [new SourceTreeNode("1", "Root", [], true)]);
        SourceTreeNode.Filter(tree, "Page 2");
        SourceTreeNode[] all = SourceTreeNode.Flatten(tree).ToArray();
        Assert.IsTrue(all.Single(node => node.PageId == "1").IsVisible);
        Assert.IsTrue(all.Single(node => node.PageId == "1").IsSelected);
        Assert.IsFalse(all.Single(node => node.PageId == "3").IsVisible);
        SourceTreeNode.Filter(tree, string.Empty);
        Assert.IsTrue(all.All(node => node.IsVisible));
    }

    [TestMethod]
    public void MalformedCycleCannotHidePagesOrLoopTheTree()
    {
        IReadOnlyList<SourceTreeNode> tree = SourceTreeNode.Build([Page("1", "3"), Page("2", "1"), Page("3", "2")], []);
        Assert.HasCount(3, SourceTreeNode.Flatten(tree));
    }
}
