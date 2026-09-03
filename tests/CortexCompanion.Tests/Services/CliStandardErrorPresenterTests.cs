// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Services;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class CliStandardErrorPresenterTests
{
    [TestMethod]
    public void LogRecordsNeverReachTheUserFacingSentence()
    {
        string standardError = string.Join(
            '\n',
            "2026-09-03T11:44:05+0200 INFO cortex.ingestion.credentials credential_read_succeeded target=cortex-spike",
            "2026-09-03T11:44:05+0200 ERROR cortex.confluence_writer.cli confluence_resolve_outside_allowlist",
            "Cortex Confluence error: Resolved page belongs to a space outside the allowlist.");

        string sentence = CliStandardErrorPresenter.UserFacing(standardError);

        Assert.AreEqual(
            "Cortex Confluence error: Resolved page belongs to a space outside the allowlist.",
            sentence);
    }

    [TestMethod]
    public void SeveralUserFacingLinesStayInOrderOnOneLine()
    {
        string sentence = CliStandardErrorPresenter.UserFacing("premiere ligne\r\nseconde ligne\r\n");

        Assert.AreEqual("premiere ligne seconde ligne", sentence);
    }

    [TestMethod]
    public void AStreamOfLogRecordsAloneLeavesNothingToShow()
    {
        string sentence = CliStandardErrorPresenter.UserFacing(
            "2026-09-03T11:44:05+0200 INFO cortex.something happened");

        Assert.AreEqual(string.Empty, sentence);
    }

    [TestMethod]
    public void AnEmptyStreamIsNotAnError()
    {
        Assert.AreEqual(string.Empty, CliStandardErrorPresenter.UserFacing(null));
        Assert.AreEqual(string.Empty, CliStandardErrorPresenter.UserFacing("   "));
    }
}
