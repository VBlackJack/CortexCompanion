// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;
using CortexCompanion.Models;
using CortexCompanion.Services;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class CliVersionPolicyTests
{
    [TestMethod]
    public void TheAcceptedCliFloorIsTheCliThisBuildShipsWith()
    {
        // Cortex and Companion are installed together by one installer, so the floor is the
        // paired CLI. Left behind, it accepts a CLI that no longer answers the way this build
        // expects, and the mismatch reaches the user as an unexplained parse failure instead
        // of the version refusal it is. Tying it here makes the release move it.
        Assert.AreEqual(
            AppConstants.MinSupportedCliVersion,
            CompanionVersionProvider.GetCurrent());
    }

    private readonly CliVersionPolicy _policy = new();

    [TestMethod]
    public void TryParseObservedFormatReturnsVersion()
    {
        bool parsed = _policy.TryParse("2026.0716.01\r\n", out CliVersion version);

        Assert.IsTrue(parsed);
        Assert.AreEqual(new CliVersion(2026, 7, 16, 1), version);
        Assert.AreEqual("2026.0716.01", version.ToString());
    }

    [TestMethod]
    public void IsSupportedOlderVersionReturnsFalse()
    {
        CliVersion minimum = new(2026, 7, 16, 1);
        CliVersion older = new(2026, 7, 15, 99);

        Assert.IsFalse(CliVersionPolicy.IsSupported(older, minimum));
    }

    [TestMethod]
    [DataRow("cortex 2026.0716.01")]
    [DataRow("2026.1316.01")]
    [DataRow("2026.0716")]
    [DataRow("")]
    public void TryParseNonParsableOutputReturnsFalse(string output)
    {
        bool parsed = _policy.TryParse(output, out _);

        Assert.IsFalse(parsed);
    }
}
