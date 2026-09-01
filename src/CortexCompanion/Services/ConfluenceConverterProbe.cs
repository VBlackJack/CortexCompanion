// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text.Json;
using CortexCompanion.Constants;
using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Authenticates the windowless converter contract before any path is persisted.</summary>
public sealed class ConfluenceConverterProbe
{
    private const string ProbeArgument = "--probe";
    private const int SupportedSchemaVersion = 1;
    private const int MaximumProbeCharacters = 4_096;
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(5);
    private readonly IProcessRunner _processRunner;

    /// <summary>Initializes the bounded process probe.</summary>
    public ConfluenceConverterProbe(IProcessRunner processRunner)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
    }

    /// <summary>Returns a normalized path only after the exact capability handshake succeeds.</summary>
    public async Task<string> ValidateAsync(string value, CancellationToken cancellationToken)
    {
        string fullPath = NormalizeExecutablePath(value);
        ProcessRunResult result = await _processRunner.RunAsync(
            new ProcessRequest(
                fullPath,
                [ProbeArgument],
                ProbeTimeout,
                MaximumProbeCharacters),
            cancellationToken);
        if (result.TimedOut || result.OutcomeUnknown || result.LaunchError is not null ||
            result.ExitCode != AppConstants.CliExitSuccess ||
            !TryValidatePayload(result.StandardOutput))
        {
            throw new ConfluenceSetupValidationException(UiStrings.ConfluenceSetupIncompatibleConverter);
        }

        return fullPath;
    }

    private static string NormalizeExecutablePath(string value)
    {
        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(value.Trim());
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new ConfluenceSetupValidationException(UiStrings.ConfluenceSetupInvalidConverter, exception);
        }

        if (!File.Exists(fullPath) ||
            !string.Equals(Path.GetExtension(fullPath), ".exe", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConfluenceSetupValidationException(UiStrings.ConfluenceSetupInvalidConverter);
        }

        return fullPath;
    }

    private static bool TryValidatePayload(string standardOutput)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(standardOutput);
            JsonElement root = document.RootElement;
            return root.ValueKind == JsonValueKind.Object &&
                root.EnumerateObject().Count() == 2 &&
                root.TryGetProperty("tool_version", out JsonElement toolVersion) &&
                toolVersion.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(toolVersion.GetString()) &&
                root.TryGetProperty("schema_version", out JsonElement schemaVersion) &&
                schemaVersion.ValueKind == JsonValueKind.Number &&
                schemaVersion.TryGetInt32(out int version) &&
                version == SupportedSchemaVersion;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
