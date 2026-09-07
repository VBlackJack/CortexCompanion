// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Globalization;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace CortexCompanion.Tests;

/// <summary>Fixes the language the assertions read, whatever the machine's own language.</summary>
[TestClass]
public static class TestCulture
{
    /// <summary>The language the interface assertions are written against.</summary>
    public const string AssertedLanguage = "fr";

    /// <summary>Pins the interface language before any localized value is read.</summary>
    /// <remarks>
    /// Many assertions quote the exact sentence a user reads, and Companion now ships two
    /// languages, so those assertions only mean something against a stated one. Without this
    /// they pass on a French machine and fail on an English one, which is precisely how a
    /// green local run reached a red build.
    ///
    /// This runs before any test touches UiStrings, which matters: several of its members are
    /// static formats parsed once when the type is first loaded. That is the same constraint
    /// the application lives under, and the reason the language setting applies at the next
    /// start rather than in place.
    /// </remarks>
    /// <param name="context">Supplied by the test framework; unused.</param>
    [AssemblyInitialize]
    public static void PinInterfaceLanguage(TestContext context)
    {
        CultureInfo culture = new(AssertedLanguage);
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}
