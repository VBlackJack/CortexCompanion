// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;

namespace CortexCompanion.Services;

/// <summary>Resolves the converter embedded beside the installed Cortex payload.</summary>
public static class ConfluenceConverterPathResolver
{
    private const string ConverterDirectoryName = "Converters";

    /// <summary>Returns the stable installer-owned converter path.</summary>
    public static string ResolveDefault() => ResolveDefaultFromDirectory(
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..")));

    /// <summary>Returns the converter installed beside one validated Cortex CLI.</summary>
    public static string ResolveDefault(string cortexCliPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cortexCliPath);
        string fullCliPath = Path.GetFullPath(cortexCliPath);
        string cortexDirectory = Path.GetDirectoryName(fullCliPath) ??
            throw new ArgumentException(
                "The Cortex CLI path has no parent directory.",
                nameof(cortexCliPath));
        return ResolveDefaultFromDirectory(cortexDirectory);
    }

    private static string ResolveDefaultFromDirectory(string cortexDirectory)
    {
        return Path.Combine(
            cortexDirectory,
            ConverterDirectoryName,
            AppConstants.ConfluenceConverterExecutableName);
    }
}
