// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class ConfluenceCredentialTargetProviderTests
{
    [TestMethod]
    public async Task MissingConfigurationUsesTheSameDefaultTargetAsCortex()
    {
        using TemporaryDirectory temporary = new();
        string cliPath = temporary.CreateFakeCli();
        Dictionary<string, string?> environment = new(StringComparer.OrdinalIgnoreCase)
        {
            ["CORTEX_CONFLUENCE_CONFIG_PATH"] = Path.Combine(temporary.Path, "missing.toml"),
        };
        ConfluenceCredentialTargetProvider provider = new(environment);

        string? target = await provider.GetTargetAsync(cliPath);

        Assert.AreEqual(AppConstants.DefaultConfluenceCredentialTarget, target);
    }
}
