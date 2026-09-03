// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Models;

/// <summary>
/// Represents the validated, environment-free contents of one Confluence TOML snapshot.
/// </summary>
public sealed record ConfluenceConfiguration(
    int SchemaVersion,
    string? BaseUrl,
    string CredentialTarget,
    DateTimeOffset? AuthExpiresAt,
    string? ConsolePath,
    int MaxAttachmentSizeMb,
    double FailureThreshold,
    IReadOnlyList<ConfluenceSpaceConfiguration> Spaces)
{
    /// <summary>Creates schema v2 while preserving every schema v1 space as whole-space collection.</summary>
    public ConfluenceConfiguration MigrateToVersionTwo() => MigrateToSchema(2);

    /// <summary>Raises the schema to the version one selection mode needs, never lowering it.</summary>
    public ConfluenceConfiguration MigrateToSchema(int targetVersion)
    {
        if (SchemaVersion >= targetVersion)
        {
            return this;
        }

        ConfluenceConfiguration migrated = this;
        if (migrated.SchemaVersion == 1)
        {
            IReadOnlyList<ConfluenceSpaceConfiguration> migratedSpaces = migrated.Spaces
                .Select(space => space with
                {
                    Selection = ConfluenceSelection.WholeSpace,
                    PageIds = Array.Empty<string>(),
                })
                .ToArray();
            migrated = migrated with { SchemaVersion = 2, Spaces = migratedSpaces };
        }

        return migrated.SchemaVersion >= targetVersion
            ? migrated
            : migrated with { SchemaVersion = targetVersion };
    }

    /// <summary>Appends one space to the allowlist without changing any unrelated value.</summary>
    public ConfluenceConfiguration AddSpace(ConfluenceSpaceConfiguration space)
    {
        ArgumentNullException.ThrowIfNull(space);
        if (Spaces.Any(existing =>
            string.Equals(existing.SpaceKey, space.SpaceKey, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("The space is already allowlisted.", nameof(space));
        }

        return this with { Spaces = Spaces.Append(space).ToArray() };
    }

    /// <summary>Replaces one space without changing any unrelated configuration value.</summary>
    public ConfluenceConfiguration ReplaceSpace(ConfluenceSpaceConfiguration replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);

        bool found = false;
        ConfluenceSpaceConfiguration[] updated = Spaces
            .Select(space =>
            {
                if (!string.Equals(space.SpaceKey, replacement.SpaceKey, StringComparison.OrdinalIgnoreCase))
                {
                    return space;
                }

                found = true;
                return replacement;
            })
            .ToArray();
        if (!found)
        {
            throw new ArgumentException("The replacement space does not exist.", nameof(replacement));
        }

        return this with { Spaces = updated };
    }

    /// <summary>Compares semantic values without relying on collection reference equality.</summary>
    public bool SemanticallyEquals(ConfluenceConfiguration? other)
    {
        if (other is null ||
            SchemaVersion != other.SchemaVersion ||
            !string.Equals(BaseUrl, other.BaseUrl, StringComparison.Ordinal) ||
            !string.Equals(CredentialTarget, other.CredentialTarget, StringComparison.Ordinal) ||
            AuthExpiresAt != other.AuthExpiresAt ||
            !string.Equals(ConsolePath, other.ConsolePath, StringComparison.Ordinal) ||
            MaxAttachmentSizeMb != other.MaxAttachmentSizeMb ||
            !FailureThreshold.Equals(other.FailureThreshold) ||
            Spaces.Count != other.Spaces.Count)
        {
            return false;
        }

        return Spaces.Zip(other.Spaces).All(pair => pair.First.SemanticallyEquals(pair.Second));
    }
}

/// <summary>Represents one validated Confluence space mapping.</summary>
public sealed record ConfluenceSpaceConfiguration(
    string SpaceKey,
    string Target,
    string Classification,
    ConfluenceSelection Selection,
    IReadOnlyList<string> PageIds)
{
    /// <summary>Compares semantic values without relying on collection reference equality.</summary>
    public bool SemanticallyEquals(ConfluenceSpaceConfiguration? other) =>
        other is not null &&
        string.Equals(SpaceKey, other.SpaceKey, StringComparison.Ordinal) &&
        string.Equals(Target, other.Target, StringComparison.Ordinal) &&
        string.Equals(Classification, other.Classification, StringComparison.Ordinal) &&
        Selection == other.Selection &&
        PageIds.SequenceEqual(other.PageIds, StringComparer.Ordinal);
}

/// <summary>Defines the explicit schema v2 collection mode.</summary>
public enum ConfluenceSelection
{
    /// <summary>Collects every page in the allowlisted space.</summary>
    WholeSpace,

    /// <summary>Collects only explicitly listed page identifiers.</summary>
    Pages,

    /// <summary>Collects every listed root together with its current descendants.</summary>
    Subtree,
}

/// <summary>Couples exact source bytes, their CAS hash, and the validated model.</summary>
public sealed record ConfluenceConfigSnapshot(
    byte[] Content,
    string ContentHash,
    ConfluenceConfiguration Configuration);
