// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>
/// Validates that CLI discovery is explicit, absolute, and points to the expected executable.
/// </summary>
public static class CliPathValidator
{
    /// <summary>Validates a configured path without attempting PATH resolution.</summary>
    public static CliPathValidationResult Validate(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return new CliPathValidationResult(CliPathValidationStatus.Missing, null);
        }

        bool isFullyQualified;
        try
        {
            isFullyQualified = Path.IsPathFullyQualified(configuredPath);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            return new CliPathValidationResult(CliPathValidationStatus.InvalidPath, null);
        }

        if (!isFullyQualified)
        {
            return new CliPathValidationResult(CliPathValidationStatus.Relative, null);
        }

        string absolutePath;
        try
        {
            absolutePath = Path.GetFullPath(configuredPath);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return new CliPathValidationResult(CliPathValidationStatus.InvalidPath, null);
        }
        if (!string.Equals(
            Path.GetFileName(absolutePath),
            AppConstants.CliExecutableName,
            StringComparison.OrdinalIgnoreCase))
        {
            return new CliPathValidationResult(CliPathValidationStatus.WrongFileName, null);
        }

        if (!File.Exists(absolutePath))
        {
            return new CliPathValidationResult(CliPathValidationStatus.FileNotFound, null);
        }

        return new CliPathValidationResult(CliPathValidationStatus.Valid, absolutePath);
    }
}
