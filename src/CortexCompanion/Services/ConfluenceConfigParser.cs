// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using CortexCompanion.Models;
using Tomlyn;
using Tomlyn.Model;

namespace CortexCompanion.Services;

/// <summary>Parses and validates raw Confluence TOML without applying environment overrides.</summary>
public sealed partial class ConfluenceConfigParser
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly HashSet<string> RootKeys =
    [
        "schema_version", "base_url", "credential_target", "auth_expires_at", "console_path",
        "max_attachment_size_mb", "failure_threshold", "spaces",
    ];
    private static readonly HashSet<string> SpaceKeys =
    [
        "space_key", "target", "classification", "selection", "pages",
    ];
    private static readonly HashSet<string> PageKeys = ["page_id"];

    /// <summary>Parses exact UTF-8 bytes into the frozen schema v1/v2 model.</summary>
    public static ConfluenceConfiguration Parse(byte[] content, string sourcePath)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        try
        {
            string text = StrictUtf8.GetString(content);
            TomlTable root = TomlSerializer.Deserialize<TomlTable>(text)
                ?? throw Invalid(sourcePath, "The TOML document is empty.");
            RejectUnknown(root, RootKeys, "configuration", sourcePath);

            int schemaVersion = OptionalInteger(root, "schema_version", 1, sourcePath);
            if (schemaVersion is not 1 and not 2)
            {
                throw Invalid(sourcePath, $"Unsupported schema_version={schemaVersion}; expected 1 or 2.");
            }

            string? baseUrl = OptionalString(root, "base_url", sourcePath);
            if (baseUrl is not null)
            {
                baseUrl = ValidateBaseUrl(baseUrl, sourcePath);
            }

            string credentialTarget = OptionalString(root, "credential_target", sourcePath) ?? "cortex-spike";
            if (string.IsNullOrWhiteSpace(credentialTarget) || credentialTarget.Any(character => character < 32))
            {
                throw Invalid(sourcePath, "credential_target must be a non-empty printable value.");
            }

            DateTimeOffset? authExpiresAt = ParseOptionalDateTime(root, "auth_expires_at", sourcePath);
            string? consolePath = OptionalString(root, "console_path", sourcePath);
            if (consolePath?.Length == 0)
            {
                consolePath = ".";
            }
            int maxAttachmentSize = OptionalInteger(root, "max_attachment_size_mb", 50, sourcePath);
            if (maxAttachmentSize < 1)
            {
                throw Invalid(sourcePath, "max_attachment_size_mb must be at least 1.");
            }

            double failureThreshold = OptionalNumber(root, "failure_threshold", 0.1, sourcePath);
            if (!double.IsFinite(failureThreshold) || failureThreshold is < 0 or > 1)
            {
                throw Invalid(sourcePath, "failure_threshold must be between 0 and 1.");
            }

            IReadOnlyList<ConfluenceSpaceConfiguration> spaces = ParseSpaces(root, schemaVersion, sourcePath);
            EnsureUnique(spaces.Select(space => space.SpaceKey), "space_key", sourcePath);
            EnsureUnique(spaces.Select(space => space.Target), "target", sourcePath);
            return new ConfluenceConfiguration(
                schemaVersion,
                baseUrl,
                credentialTarget,
                authExpiresAt,
                consolePath,
                maxAttachmentSize,
                failureThreshold,
                spaces);
        }
        catch (DecoderFallbackException exception)
        {
            throw Invalid(sourcePath, "The file is not strict UTF-8.", exception);
        }
        catch (Exception exception) when (exception is not ConfluenceConfigValidationException)
        {
            throw Invalid(sourcePath, "Could not parse valid Confluence TOML.", exception);
        }
    }

    private static IReadOnlyList<ConfluenceSpaceConfiguration> ParseSpaces(
        TomlTable root,
        int schemaVersion,
        string sourcePath)
    {
        if (!root.TryGetValue("spaces", out object? rawSpaces))
        {
            return Array.Empty<ConfluenceSpaceConfiguration>();
        }

        if (rawSpaces is not TomlTableArray spaces)
        {
            throw Invalid(sourcePath, "spaces must be an array of tables.");
        }

        List<ConfluenceSpaceConfiguration> result = [];
        foreach (TomlTable space in spaces)
        {
            RejectUnknown(space, SpaceKeys, "space", sourcePath);
            string spaceKey = RequiredString(space, "space_key", sourcePath);
            if (!SpaceKeyPattern().IsMatch(spaceKey))
            {
                throw Invalid(sourcePath, "space_key must contain only letters, digits, dot, dash, or underscore.");
            }

            string target = RequiredString(space, "target", sourcePath);
            ValidateTarget(target, sourcePath);
            string classification = RequiredString(space, "classification", sourcePath);
            if (classification is not "perso-non-sensible" and not "pro-confidentiel")
            {
                throw Invalid(sourcePath, "classification has an unsupported value.");
            }

            bool hasSelection = space.TryGetValue("selection", out object? selectionValue);
            bool hasPages = space.TryGetValue("pages", out object? pagesValue);
            if (schemaVersion == 1 && (hasSelection || hasPages))
            {
                throw Invalid(sourcePath, "schema_version=1 spaces must use the legacy whole-space shape.");
            }

            ConfluenceSelection selection = ConfluenceSelection.WholeSpace;
            if (schemaVersion == 2)
            {
                if (!hasSelection || selectionValue is not string selectionText)
                {
                    throw Invalid(sourcePath, "schema_version=2 requires selection for every space.");
                }

                selection = selectionText switch
                {
                    "whole_space" => ConfluenceSelection.WholeSpace,
                    "pages" => ConfluenceSelection.Pages,
                    _ => throw Invalid(sourcePath, "selection must be 'whole_space' or 'pages'."),
                };
            }

            if (selection == ConfluenceSelection.WholeSpace && hasPages)
            {
                throw Invalid(sourcePath, "selection='whole_space' must not include a pages table.");
            }

            IReadOnlyList<string> pageIds = selection == ConfluenceSelection.Pages
                ? ParsePages(pagesValue, hasPages, sourcePath)
                : Array.Empty<string>();
            result.Add(new ConfluenceSpaceConfiguration(spaceKey, target, classification, selection, pageIds));
        }

        return result;
    }

    private static IReadOnlyList<string> ParsePages(object? value, bool present, string sourcePath)
    {
        if (!present)
        {
            return Array.Empty<string>();
        }

        List<string> pageIds = [];
        if (value is TomlTableArray tables)
        {
            foreach (TomlTable page in tables)
            {
                RejectUnknown(page, PageKeys, "page", sourcePath);
                string pageId = RequiredString(page, "page_id", sourcePath);
                if (!PageIdPattern().IsMatch(pageId))
                {
                    throw Invalid(sourcePath, "page_id must be a non-empty numeric string.");
                }

                pageIds.Add(pageId);
            }
        }
        else if (value is TomlArray array && array.Count == 0)
        {
            return Array.Empty<string>();
        }
        else
        {
            throw Invalid(sourcePath, "pages must be an empty array or an array of tables.");
        }

        EnsureUnique(pageIds, "page_id", sourcePath, StringComparer.Ordinal);
        return pageIds;
    }

    private static string ValidateBaseUrl(string value, string sourcePath)
    {
        string normalized = value.TrimEnd('/');
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrEmpty(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            throw Invalid(sourcePath, "base_url must be an HTTP(S) URL without credentials or query.");
        }

        return normalized;
    }

    private static void ValidateTarget(string value, string sourcePath)
    {
        string normalized = value.Replace('\\', '/');
        string[] parts = value.Split('/');
        if (string.IsNullOrEmpty(value) || value is "." or ".." ||
            value.Contains('\\', StringComparison.Ordinal) || value.Contains('\0', StringComparison.Ordinal) ||
            Path.IsPathFullyQualified(value) || WindowsDrivePattern().IsMatch(value) ||
            parts.Any(part => part is "" or "." or "..") || normalized != value)
        {
            throw Invalid(sourcePath, "target must be a normalized relative POSIX directory.");
        }
    }

    private static DateTimeOffset? ParseOptionalDateTime(TomlTable table, string key, string sourcePath)
    {
        if (!table.TryGetValue(key, out object? raw))
        {
            return null;
        }

        if (raw is TomlDateTime tomlDateTime)
        {
            if (tomlDateTime.Kind is TomlDateTimeKind.OffsetDateTimeByNumber or
                TomlDateTimeKind.OffsetDateTimeByZ)
            {
                return tomlDateTime.DateTime;
            }

            throw Invalid(sourcePath, "auth_expires_at must include a UTC offset.");
        }

        if (raw is not string value)
        {
            throw Invalid(sourcePath, "auth_expires_at must be a datetime or string.");
        }

        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset parsed) ||
            !OffsetPattern().IsMatch(value))
        {
            throw Invalid(sourcePath, "auth_expires_at must include a UTC offset.");
        }

        return parsed;
    }

    private static int OptionalInteger(TomlTable table, string key, int defaultValue, string sourcePath)
    {
        if (!table.TryGetValue(key, out object? value))
        {
            return defaultValue;
        }

        return value is long integer && integer is >= int.MinValue and <= int.MaxValue
            ? (int)integer
            : throw Invalid(sourcePath, $"{key} must be an integer.");
    }

    private static double OptionalNumber(TomlTable table, string key, double defaultValue, string sourcePath)
    {
        if (!table.TryGetValue(key, out object? value))
        {
            return defaultValue;
        }

        return value switch
        {
            long integer => integer,
            double number => number,
            _ => throw Invalid(sourcePath, $"{key} must be a number."),
        };
    }

    private static string RequiredString(TomlTable table, string key, string sourcePath) =>
        OptionalString(table, key, sourcePath)
        ?? throw Invalid(sourcePath, $"{key} is required.");

    private static string? OptionalString(TomlTable table, string key, string sourcePath)
    {
        if (!table.TryGetValue(key, out object? value))
        {
            return null;
        }

        return value as string ?? throw Invalid(sourcePath, $"{key} must be a string.");
    }

    private static void RejectUnknown(
        TomlTable table,
        HashSet<string> allowed,
        string context,
        string sourcePath)
    {
        string[] unknown = table.Keys.Where(key => !allowed.Contains(key)).Order().ToArray();
        if (unknown.Length > 0)
        {
            throw Invalid(sourcePath, $"Unknown {context} key(s): {string.Join(", ", unknown)}.");
        }
    }

    private static void EnsureUnique(
        IEnumerable<string> values,
        string field,
        string sourcePath,
        StringComparer? comparer = null)
    {
        StringComparer effectiveComparer = comparer ?? StringComparer.OrdinalIgnoreCase;
        HashSet<string> seen = new(effectiveComparer);
        if (values.Any(value => !seen.Add(value)))
        {
            throw Invalid(sourcePath, $"Configuration must not contain duplicate {field} values.");
        }
    }

    private static ConfluenceConfigValidationException Invalid(
        string sourcePath,
        string message,
        Exception? innerException = null) =>
        new($"Invalid Confluence configuration at '{sourcePath}': {message}", innerException);

    [GeneratedRegex("^[A-Za-z0-9._-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SpaceKeyPattern();

    [GeneratedRegex("^[0-9]+$", RegexOptions.CultureInvariant)]
    private static partial Regex PageIdPattern();

    [GeneratedRegex("(?:Z|[+-][0-9]{2}:[0-9]{2})$", RegexOptions.CultureInvariant)]
    private static partial Regex OffsetPattern();

    [GeneratedRegex("^[A-Za-z]:", RegexOptions.CultureInvariant)]
    private static partial Regex WindowsDrivePattern();
}

/// <summary>Reports a strict TOML validation failure without exposing implementation details to the UI.</summary>
public sealed class ConfluenceConfigValidationException : Exception
{
    /// <summary>Initializes a validation error.</summary>
    public ConfluenceConfigValidationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
