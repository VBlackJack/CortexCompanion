// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Models;

/// <summary>Captures the non-secret values required to initialize Confluence safely.</summary>
public sealed record ConfluenceSetupRequest(
    string PageUrl,
    string SpaceKey,
    DateTimeOffset AuthExpiresAt,
    string? ConsolePath,
    string Classification);

/// <summary>Describes the configuration values inferred from one supported page URL.</summary>
public sealed record ConfluencePageUrlAnalysis(
    string BaseUrl,
    string? InferredSpaceKey);

/// <summary>Couples one persisted classification code with its localized display label.</summary>
public sealed record ConfluenceClassificationOption(
    string Code,
    string DisplayName);
