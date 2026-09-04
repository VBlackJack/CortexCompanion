// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Services;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class SyncProgressParserTests
{
    [TestMethod]
    public void LatestCompleteRecordWinsAmongDiagnosticsAndPartialWrites()
    {
        string stderr = """
            normal diagnostic
            CORTEX_PROGRESS {"contract_version":1,"current":700,"phase":"staging","total":1594}
            CORTEX_PROGRESS {"contract_version":1,"current":701
            """;

        SyncProgressRecord? progress = SyncProgressParser.ReadLatest(stderr);

        Assert.IsNotNull(progress);
        Assert.AreEqual("staging", progress.Phase);
        Assert.AreEqual(700, progress.Current);
        Assert.AreEqual(1594, progress.Total);
    }

    [TestMethod]
    public void UnknownOrInvalidRecordsAreIgnored()
    {
        Assert.IsNull(SyncProgressParser.ReadLatest(
            "CORTEX_PROGRESS {\"contract_version\":1,\"current\":3,\"phase\":\"other\",\"total\":2}"));
    }

    [TestMethod]
    public void LocalIndexationPhaseIsAccepted()
    {
        SyncProgressRecord? progress = SyncProgressParser.ReadLatest(
            "CORTEX_PROGRESS {\"contract_version\":1,\"current\":12,\"phase\":\"indexation\",\"total\":340}");

        Assert.IsNotNull(progress);
        Assert.AreEqual("indexation", progress.Phase);
        Assert.AreEqual(12, progress.Current);
        Assert.AreEqual(340, progress.Total);
    }
}
