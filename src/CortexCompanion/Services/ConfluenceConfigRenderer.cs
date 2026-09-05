// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Renders the deterministic UTF-8/LF TOML representation owned by Cortex.</summary>
public sealed class ConfluenceConfigRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Renders validated schema v1 or v2 settings with the frozen Python field order.</summary>
    public static byte[] Render(ConfluenceConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        List<string> lines = [$"schema_version = {configuration.SchemaVersion}"];
        if (configuration.BaseUrl is not null)
        {
            lines.Add($"base_url = {Quote(configuration.BaseUrl)}");
        }

        lines.Add($"credential_target = {Quote(configuration.CredentialTarget)}");
        if (configuration.AuthExpiresAt is not null)
        {
            lines.Add($"auth_expires_at = {Quote(FormatDateTime(configuration.AuthExpiresAt.Value))}");
        }

        if (configuration.ConsolePath is not null)
        {
            lines.Add($"console_path = {Quote(configuration.ConsolePath)}");
        }

        lines.Add($"max_attachment_size_mb = {configuration.MaxAttachmentSizeMb.ToString(CultureInfo.InvariantCulture)}");
        lines.Add($"failure_threshold = {FormatFloat(configuration.FailureThreshold)}");
        foreach (ConfluenceSpaceConfiguration space in configuration.Spaces)
        {
            lines.Add(string.Empty);
            lines.Add("[[spaces]]");
            lines.Add($"space_key = {Quote(space.SpaceKey)}");
            lines.Add($"target = {Quote(space.Target)}");
            lines.Add($"classification = {Quote(space.Classification)}");
            if (configuration.SchemaVersion == 1)
            {
                continue;
            }

            lines.Add($"selection = {Quote(space.Selection switch
            {
                ConfluenceSelection.Pages => "pages",
                ConfluenceSelection.Subtree => "subtree",
                _ => "whole_space",
            })}");
            if (space.Selection == ConfluenceSelection.WholeSpace)
            {
                continue;
            }

            if (space.PageIds.Count == 0)
            {
                // Match Cortex's canonical spelling for every empty explicit selection.
                lines.Add("pages = []");
                continue;
            }

            foreach (string pageId in space.PageIds)
            {
                lines.Add(string.Empty);
                lines.Add("[[spaces.pages]]");
                lines.Add($"page_id = {Quote(pageId)}");
            }
        }

        return new UTF8Encoding(false).GetBytes(string.Join('\n', lines) + "\n");
    }

    private static string Quote(string value) => JsonSerializer.Serialize(value, JsonOptions);

    private static string FormatDateTime(DateTimeOffset value)
    {
        string formatted = value.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFzzz", CultureInfo.InvariantCulture);
        return formatted.EndsWith("+00:00", StringComparison.Ordinal)
            ? formatted[..^6] + "+00:00"
            : formatted;
    }

    private static string FormatFloat(double value)
    {
        string formatted = value.ToString("R", CultureInfo.InvariantCulture).Replace('E', 'e');
        if (!formatted.Contains('.', StringComparison.Ordinal) && !formatted.Contains('e', StringComparison.Ordinal))
        {
            formatted += ".0";
        }

        return formatted;
    }
}
