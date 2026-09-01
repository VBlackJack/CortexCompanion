// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CortexCompanion.Constants;
using CortexCompanion.Logging;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Reads the atomic source-health snapshot directly without invoking Cortex.</summary>
public static class IngestionHealthReader
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    /// <summary>Reads and validates the complete schema-v1 document health snapshot.</summary>
    public static async Task<IngestionHealthReadResult> ReadAsync(
        string healthPath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(healthPath);
        if (!File.Exists(healthPath))
        {
            return new IngestionHealthReadResult(IngestionHealthReadState.Missing, null, null);
        }

        try
        {
            await using FileStream stream = new(
                healthPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 4_096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            IngestionHealthSnapshot? snapshot = await JsonSerializer.DeserializeAsync<IngestionHealthSnapshot>(
                stream,
                JsonOptions,
                cancellationToken);
            Validate(snapshot);
            return new IngestionHealthReadResult(IngestionHealthReadState.Loaded, snapshot, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                                          JsonException or IngestionHealthValidationException)
        {
            FileLogger.Error("Ingestion health snapshot could not be read", exception);
            return new IngestionHealthReadResult(
                IngestionHealthReadState.Unreadable,
                null,
                exception.GetType().Name);
        }
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = false,
        };
        options.Converters.Add(new StrictDateTimeOffsetConverter());
        options.Converters.Add(new StrictNullableDateTimeOffsetConverter());
        return options;
    }

    private static void Validate(IngestionHealthSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.SchemaVersion != 1 ||
            !string.Equals(snapshot.SourceKind, AppConstants.IngestionSourceKind, StringComparison.Ordinal) ||
            snapshot.Status is not "ok" and not "degraded" and not "error" ||
            snapshot.Counts is null ||
            snapshot.Counts.Seen < 0 || snapshot.Counts.Converted < 0 || snapshot.Counts.Failed < 0 ||
            snapshot.Counts.CarryForward < 0 || snapshot.Counts.Tombstones < 0 ||
            snapshot.SelectionFingerprint is not null &&
                (snapshot.SelectionFingerprint.Length != 64 ||
                 snapshot.SelectionFingerprint.Any(character =>
                    !char.IsAsciiDigit(character) && character is < 'a' or > 'f')) ||
            snapshot.ScopeSummaries.Any(summary =>
                summary.SpaceKey.Length == 0 ||
                summary.Selection is not "whole_space" and not "pages" and not "subtree" ||
                summary.SelectedPageCount < 0 ||
                summary.AvailablePageCount < 0 ||
                summary.ExcludedDescendantCount < 0))
        {
            throw new IngestionHealthValidationException();
        }
    }

    private sealed class StrictDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            string? value = reader.GetString();
            return TryParse(value, out DateTimeOffset parsed)
                ? parsed
                : throw new JsonException("A source-health timestamp lacks a UTC offset.");
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value);
    }

    private sealed class StrictNullableDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
    {
        public override DateTimeOffset? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            string? value = reader.GetString();
            return TryParse(value, out DateTimeOffset parsed)
                ? parsed
                : throw new JsonException("A source-health timestamp lacks a UTC offset.");
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(value.Value);
        }
    }

    private static bool TryParse(string? value, out DateTimeOffset parsed)
    {
        parsed = default;
        bool hasOffset = value is not null &&
            (value.EndsWith('Z') ||
             (value.Length >= 6 && (value[^6] == '+' || value[^6] == '-') && value[^3] == ':'));
        return hasOffset && DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out parsed);
    }
}

/// <summary>Marks a complete source-health contract validation failure.</summary>
public sealed class IngestionHealthValidationException : Exception;
