// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace CortexCompanion.Services;

/// <summary>Extracts only the latest stable Cortex progress record from stderr.</summary>
public static class SyncProgressParser
{
    private const string Prefix = "CORTEX_PROGRESS ";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
    };

    /// <summary>Returns the latest complete and validated progress record.</summary>
    public static SyncProgressRecord? ReadLatest(string standardError)
    {
        ArgumentNullException.ThrowIfNull(standardError);
        string[] lines = standardError.Split('\n');
        for (int index = lines.Length - 1; index >= 0; index--)
        {
            string line = lines[index].TrimEnd('\r');
            if (!line.StartsWith(Prefix, StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                SyncProgressRecord? progress = JsonSerializer.Deserialize<SyncProgressRecord>(
                    line[Prefix.Length..],
                    JsonOptions);
                if (progress is not null &&
                    progress.ContractVersion == 1 &&
                    progress.Phase is "enumeration" or "staging" or "conversion" or "publication" &&
                    progress.Current >= 0 &&
                    progress.Total >= 0 &&
                    progress.Current <= progress.Total)
                {
                    return progress;
                }
            }
            catch (JsonException)
            {
                // A concurrently written partial line is ignored until the next poll.
            }
        }

        return null;
    }
}

/// <summary>Represents one strict Cortex progress record.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SyncProgressRecord
{
    [JsonPropertyName("contract_version")]
    public required int ContractVersion { get; init; }

    [JsonPropertyName("phase")]
    public required string Phase { get; init; }

    [JsonPropertyName("current")]
    public required int Current { get; init; }

    [JsonPropertyName("total")]
    public required int Total { get; init; }
}
