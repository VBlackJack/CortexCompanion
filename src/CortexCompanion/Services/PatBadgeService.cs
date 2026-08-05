// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using CortexCompanion.Constants;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Computes the effective non-secret PAT expiry for display only.</summary>
public static class PatBadgeService
{
    private const string EnvironmentName = "CORTEX_CONFLUENCE_AUTH_EXPIRES_AT";

    /// <summary>Reads environment over raw TOML and classifies the configured expiry.</summary>
    public static async Task<PatBadgeResult> ReadAsync(
        string? configPath,
        DateTimeOffset now,
        Func<string, string?>? readEnvironment = null,
        CancellationToken cancellationToken = default)
    {
        Func<string, string?> reader = readEnvironment ?? Environment.GetEnvironmentVariable;
        string? environmentValue = reader(EnvironmentName)?.Trim();
        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return TryCreate(environmentValue, EnvironmentName, now);
        }

        if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
        {
            return new PatBadgeResult(PatBadgeState.Unknown, null, null, null);
        }

        try
        {
            byte[] content = await File.ReadAllBytesAsync(configPath, cancellationToken);
            ConfluenceConfiguration configuration = ConfluenceConfigParser.Parse(content, configPath);
            return configuration.AuthExpiresAt is null
                ? new PatBadgeResult(PatBadgeState.Unknown, null, null, null)
                : Classify(configuration.AuthExpiresAt.Value, configPath, now);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                                          ConfluenceConfigValidationException)
        {
            return new PatBadgeResult(PatBadgeState.Error, null, configPath, exception.GetType().Name);
        }
    }

    private static PatBadgeResult TryCreate(string value, string origin, DateTimeOffset now)
    {
        bool hasOffset = value.EndsWith('Z') ||
            (value.Length >= 6 && (value[^6] == '+' || value[^6] == '-') && value[^3] == ':');
        if (!hasOffset || !DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTimeOffset parsed))
        {
            return new PatBadgeResult(PatBadgeState.Error, null, origin, "InvalidDateTimeOffset");
        }

        return Classify(parsed, origin, now);
    }

    private static PatBadgeResult Classify(DateTimeOffset expiresAt, string origin, DateTimeOffset now)
    {
        PatBadgeState state = expiresAt <= now
            ? PatBadgeState.Expired
            : expiresAt - now < TimeSpan.FromDays(AppConstants.PatExpiryWarningDays)
                ? PatBadgeState.Warning
                : PatBadgeState.Ok;
        return new PatBadgeResult(state, expiresAt, origin, null);
    }
}
