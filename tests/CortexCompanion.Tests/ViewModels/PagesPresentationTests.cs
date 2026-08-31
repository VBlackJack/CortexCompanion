// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;
using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Models;
using CortexCompanion.ViewModels;

namespace CortexCompanion.Tests.ViewModels;

[TestClass]
public sealed class PagesPresentationTests
{
    [TestMethod]
    public void IncompatibleHandshakeNamesOldVersionAndMinimum()
    {
        string result = UiStrings.FormatHandshakeIncompatible("2026.0804.00", AppConstants.MinSupportedCliVersion);

        StringAssert.Contains(result, "version trop ancienne");
        StringAssert.Contains(result, "détectée 2026.0804.00");
        StringAssert.Contains(result, $"minimum {AppConstants.MinSupportedCliVersion}");
    }

    [TestMethod]
    public void NullTitleAndEmptyPagesModeRemainExplicit()
    {
        ConfiguredPageViewModel page = new("DOC", "123", null, null);
        ConfiguredSpaceViewModel space = new(
            "DOC",
            "docs",
            "pro-confidentiel",
            ConfluenceSelection.Pages,
            []);

        Assert.AreEqual(UiStrings.PageTitleUnknown, page.DisplayTitle);
        Assert.AreEqual(UiStrings.PageTitleUnknownUntilSync, page.TitleProvenance);
        Assert.IsTrue(space.IsEmptyPagesSelection);
        Assert.AreEqual(UiStrings.PagesModeDescription, space.SelectionDescription);
    }

    [TestMethod]
    public void EmptyEnvironmentOverridesAreHiddenByOneExplicitProjection()
    {
        PagesViewModel viewModel = new(null, null, null, []);

        Assert.IsFalse(viewModel.HasOverrides);
    }

    [TestMethod]
    public async Task ReadOnlyInitializationDoesNotRelaunchCortexAfterFailedHandshake()
    {
        CountingCliClient client = new();
        PagesViewModel viewModel = new(client, null, null, []);

        await viewModel.InitializeAsync(isReadOnly: true);

        Assert.AreEqual(0, client.GetPagesCallCount);
        Assert.AreEqual(UiStrings.PagesReadOnly, viewModel.StateMessage);
    }

    private sealed class CountingCliClient : IConfluenceCliClient
    {
        public int GetPagesCallCount { get; private set; }

        public Task<ConfluenceCliResult<PagesContract>> GetPagesAsync(CancellationToken cancellationToken)
        {
            GetPagesCallCount++;
            return Task.FromException<ConfluenceCliResult<PagesContract>>(
                new InvalidOperationException("Pages must not be read after a failed handshake."));
        }

        public Task<ConfluenceCliResult<ResolvedPageContract>> ResolveAsync(
            string reference,
            CancellationToken cancellationToken) =>
            Task.FromException<ConfluenceCliResult<ResolvedPageContract>>(
                new InvalidOperationException("Resolve is not part of startup initialization."));
    }
}
