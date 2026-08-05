// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class CliPathValidatorTests
{
    [TestMethod]
    public void ValidateAbsentPathReturnsMissing()
    {
        CliPathValidationResult result = CliPathValidator.Validate(null);

        Assert.AreEqual(CliPathValidationStatus.Missing, result.Status);
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void ValidateRelativePathReturnsRelative()
    {
        CliPathValidationResult result = CliPathValidator.Validate("tools\\cortex.exe");

        Assert.AreEqual(CliPathValidationStatus.Relative, result.Status);
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void ValidateMissingAbsolutePathReturnsFileNotFound()
    {
        using TemporaryDirectory temporaryDirectory = new();
        string missingPath = Path.Combine(temporaryDirectory.Path, "cortex.exe");

        CliPathValidationResult result = CliPathValidator.Validate(missingPath);

        Assert.AreEqual(CliPathValidationStatus.FileNotFound, result.Status);
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void ValidateExistingAbsoluteCortexExecutableReturnsValid()
    {
        using TemporaryDirectory temporaryDirectory = new();
        string executablePath = temporaryDirectory.CreateFakeCli();

        CliPathValidationResult result = CliPathValidator.Validate(executablePath);

        Assert.AreEqual(CliPathValidationStatus.Valid, result.Status);
        Assert.AreEqual(executablePath, result.AbsolutePath);
        Assert.IsTrue(result.IsValid);
    }
}
