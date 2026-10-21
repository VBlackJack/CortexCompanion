// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Tests.TestSupport;

/// <summary>Reads JSON documents captured from the real Cortex CLI and copied next to the tests.</summary>
internal static class CliFixtures
{
    private const string FixtureDirectory = "Fixtures";

    public static string Read(params string[] relativePath) =>
        File.ReadAllText(Path.Combine([AppContext.BaseDirectory, FixtureDirectory, .. relativePath]));
}
