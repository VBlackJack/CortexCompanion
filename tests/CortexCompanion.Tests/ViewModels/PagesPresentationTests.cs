// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;
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
        StringAssert.Contains(result, "detectee 2026.0804.00");
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
}
