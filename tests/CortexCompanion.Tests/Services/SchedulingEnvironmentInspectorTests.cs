// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Services;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class SchedulingEnvironmentInspectorTests
{
    private static readonly string[] ExpectedBlockedNames =
    [
        "CORTEX_CONFLUENCE_FUTURE_SECRET",
        "CORTEX_INGESTION_DATA_ROOT",
    ];

    [TestMethod]
    public void RecognizedAndUnknownPrefixedNamesBlockWithoutExposingValues()
    {
        Dictionary<string, string?> environment = new(StringComparer.OrdinalIgnoreCase)
        {
            ["CORTEX_INGESTION_DATA_ROOT"] = @"C:\sensitive\root",
            ["CORTEX_CONFLUENCE_FUTURE_SECRET"] = "must-not-leak",
            ["CORTEX_CONFLUENCE_EMPTY"] = " ",
            ["UNRELATED"] = "value",
        };

        IReadOnlyList<string> result = SchedulingEnvironmentInspector.GetActiveVariableNames(environment);

        CollectionAssert.AreEqual(ExpectedBlockedNames, result.ToArray());
        Assert.IsFalse(string.Join("|", result).Contains("must-not-leak", StringComparison.Ordinal));
        Assert.IsFalse(string.Join("|", result).Contains("sensitive", StringComparison.Ordinal));
    }
}
