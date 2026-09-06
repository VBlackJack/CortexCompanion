// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Net.Http;
using System.Text.Json;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Checks only the official stable release and never downloads or executes an installer.</summary>
public sealed class ReleaseUpdateClient
{
    /// <summary>Gets the official release endpoint.</summary>
    public const string LatestApi = "https://api.github.com/repos/VBlackJack/Cortex/releases/latest";
    /// <summary>Gets the official common installer entry point.</summary>
    public const string ReleasesPage = "https://github.com/VBlackJack/Cortex/releases/latest";
    /// <summary>Gets the bounded network wait.</summary>
    public static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(15);
    private static readonly HttpClient SharedClient = new() { Timeout = CheckTimeout };
    private readonly HttpClient _client;

    /// <summary>Allows deterministic HTTP fixtures without contacting GitHub.</summary>
    public ReleaseUpdateClient(HttpClient? client = null) => _client = client ?? SharedClient;

    /// <summary>Validates the stable CalVer before presenting availability.</summary>
    public async Task<CliVersion> ReadLatestAsync(CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, LatestApi);
        request.Headers.UserAgent.ParseAdd("CortexCompanion");
        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(CheckTimeout);
        using HttpResponseMessage response = await _client.SendAsync(request, deadline.Token);
        response.EnsureSuccessStatusCode();
        await using Stream stream = await response.Content.ReadAsStreamAsync(deadline.Token);
        using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: deadline.Token);
        JsonElement root = document.RootElement;
        string? tag = root.GetProperty("tag_name").GetString();
        if (root.GetProperty("draft").GetBoolean() || root.GetProperty("prerelease").GetBoolean() ||
            tag is null || !tag.StartsWith('v') || !new CliVersionPolicy().TryParse(tag[1..], out CliVersion version))
        {
            throw new InvalidDataException("The stable release version is invalid.");
        }
        return version;
    }
}
