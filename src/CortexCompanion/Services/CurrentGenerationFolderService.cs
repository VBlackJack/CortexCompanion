// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using CortexCompanion.Constants;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Resolves and opens only the current immutable Cortex documents directory.</summary>
public static class CurrentGenerationFolderService
{
    /// <summary>Returns the current documents directory after strict containment validation.</summary>
    public static async Task<string> ResolveAsync(
        IngestionPathResolution pathResolution,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pathResolution);
        string sourceRoot = Path.GetFullPath(Path.Combine(
            pathResolution.DataRoot,
            AppConstants.IngestionSourceKind));
        string pointerPath = Path.Combine(sourceRoot, "current.json");
        await using FileStream stream = new(
            pointerPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 4_096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        CurrentGenerationPointer? pointer = await JsonSerializer.DeserializeAsync<CurrentGenerationPointer>(
            stream,
            cancellationToken: cancellationToken);
        if (pointer is null || pointer.SchemaVersion != 1 ||
            string.IsNullOrWhiteSpace(pointer.GenerationId) ||
            pointer.GenerationId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            pointer.GenerationId is "." or "..")
        {
            throw new InvalidDataException("The current Cortex generation pointer is invalid.");
        }

        string generationsRoot = Path.GetFullPath(Path.Combine(sourceRoot, "generations"));
        string documents = Path.GetFullPath(Path.Combine(
            generationsRoot,
            pointer.GenerationId,
            "documents"));
        string rootWithSeparator = generationsRoot.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!documents.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) ||
            !Directory.Exists(documents))
        {
            throw new DirectoryNotFoundException();
        }

        return documents;
    }

    /// <summary>Opens the validated directory in Windows Explorer.</summary>
    public static async Task OpenAsync(
        IngestionPathResolution pathResolution,
        CancellationToken cancellationToken)
    {
        string documents = await ResolveAsync(pathResolution, cancellationToken);
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            UseShellExecute = true,
            ArgumentList = { documents },
        });
    }

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    private sealed record CurrentGenerationPointer
    {
        [JsonPropertyName("schema_version")]
        public required int SchemaVersion { get; init; }

        [JsonPropertyName("generation_id")]
        public required string GenerationId { get; init; }
    }
}
