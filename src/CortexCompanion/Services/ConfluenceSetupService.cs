// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text.RegularExpressions;
using CortexCompanion.Constants;
using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Logging;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Creates the first validated Confluence TOML without ever accepting secret material.</summary>
public sealed partial class ConfluenceSetupService
{
    private const int InitialSchemaVersion = 2;
    private const int DefaultMaxAttachmentSizeMb = 50;
    private const double DefaultFailureThreshold = 0.1;
    internal const string TargetRoot = "confluence";
    private readonly IConfluenceConfigStore _configStore;
    private readonly ConfluenceConverterProbe _converterProbe;
    private readonly string _defaultConsolePath;
    private readonly string? _environmentConsolePath;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes first-run creation over the existing atomic configuration store.</summary>
    public ConfluenceSetupService(
        IConfluenceConfigStore configStore,
        TimeProvider? timeProvider = null)
        : this(
            configStore,
            new ConfluenceConverterProbe(new ProcessRunner()),
            ConfluenceConverterPathResolver.ResolveDefault(),
            environmentConsolePath: null,
            timeProvider)
    {
    }

    /// <summary>Initializes setup with an injectable converter boundary and installation path.</summary>
    public ConfluenceSetupService(
        IConfluenceConfigStore configStore,
        ConfluenceConverterProbe converterProbe,
        string defaultConsolePath,
        string? environmentConsolePath = null,
        TimeProvider? timeProvider = null)
    {
        _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
        _converterProbe = converterProbe ?? throw new ArgumentNullException(nameof(converterProbe));
        _defaultConsolePath = string.IsNullOrWhiteSpace(defaultConsolePath)
            ? throw new ArgumentException("A default converter path is required.", nameof(defaultConsolePath))
            : defaultConsolePath;
        _environmentConsolePath = string.IsNullOrWhiteSpace(environmentConsolePath)
            ? null
            : environmentConsolePath;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Gets the installer-owned converter path shown as the advanced default.</summary>
    public string DefaultConsolePath => _defaultConsolePath;

    /// <summary>Validates non-secret setup values and creates the file only when it is still absent.</summary>
    public async Task<ConfluenceConfigSnapshot> InitializeAsync(
        ConfluenceSetupRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ConfluencePageUrlAnalysis analysis = ConfluencePageUrlAnalyzer.Analyze(request.PageUrl);
        string spaceKey = request.SpaceKey.Trim();
        if (!SpaceKeyPattern().IsMatch(spaceKey))
        {
            throw new ConfluenceSetupValidationException(UiStrings.ConfluenceSetupInvalidSpaceKey);
        }

        if (analysis.InferredSpaceKey is not null &&
            !string.Equals(analysis.InferredSpaceKey, spaceKey, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConfluenceSetupValidationException(UiStrings.ConfluenceSetupSpaceMismatch);
        }

        if (request.AuthExpiresAt <= _timeProvider.GetUtcNow())
        {
            throw new ConfluenceSetupValidationException(UiStrings.ConfluenceSetupExpiredAuthentication);
        }

        if (request.Classification is not "pro-confidentiel" and not "perso-non-sensible")
        {
            throw new ConfluenceSetupValidationException(UiStrings.ConfluenceSetupInvalidClassification);
        }

        string consolePath = await ValidateSelectedOrDefaultAsync(
            request.ConsolePath,
            cancellationToken);
        await ValidateEnvironmentOverrideAsync(cancellationToken);
        ConfluenceSpaceConfiguration space = new(
            spaceKey,
            $"{TargetRoot}/{spaceKey}",
            request.Classification,
            ConfluenceSelection.Pages,
            Array.Empty<string>());
        ConfluenceConfiguration configuration = new(
            InitialSchemaVersion,
            analysis.BaseUrl,
            AppConstants.DefaultConfluenceCredentialTarget,
            request.AuthExpiresAt,
            consolePath,
            DefaultMaxAttachmentSizeMb,
            DefaultFailureThreshold,
            [space]);
        return await _configStore.WriteAsync(configuration, expectedHash: null, cancellationToken);
    }

    /// <summary>Validates and migrates an existing configuration before the CLI can read it.</summary>
    public async Task<ConfluenceConfigSnapshot> EnsureReadyAsync(
        CancellationToken cancellationToken)
    {
        ConfluenceConfigSnapshot snapshot = await _configStore.ReadAsync(cancellationToken);
        string validatedPath;
        try
        {
            validatedPath = await ValidateSelectedOrDefaultAsync(
                snapshot.Configuration.ConsolePath,
                cancellationToken);
        }
        catch (ConfluenceSetupValidationException) when (
            IsKnownWindowedExecutable(snapshot.Configuration.ConsolePath))
        {
            FileLogger.Warn("Replacing known windowed Confluence converter with embedded console");
            validatedPath = await _converterProbe.ValidateAsync(
                _defaultConsolePath,
                cancellationToken);
        }
        if (!string.Equals(
                snapshot.Configuration.ConsolePath,
                validatedPath,
                StringComparison.Ordinal))
        {
            snapshot = await _configStore.WriteAsync(
                snapshot.Configuration with { ConsolePath = validatedPath },
                snapshot.ContentHash,
                cancellationToken);
        }

        await ValidateEnvironmentOverrideAsync(cancellationToken);
        return snapshot;
    }

    /// <summary>Validates a developer-selected override before the UI accepts it.</summary>
    public Task<string> ValidateConverterAsync(
        string path,
        CancellationToken cancellationToken) =>
        _converterProbe.ValidateAsync(path, cancellationToken);

    private Task<string> ValidateSelectedOrDefaultAsync(
        string? selectedPath,
        CancellationToken cancellationToken) =>
        _converterProbe.ValidateAsync(
            string.IsNullOrWhiteSpace(selectedPath) ? _defaultConsolePath : selectedPath,
            cancellationToken);

    private async Task ValidateEnvironmentOverrideAsync(CancellationToken cancellationToken)
    {
        if (_environmentConsolePath is not null)
        {
            _ = await _converterProbe.ValidateAsync(_environmentConsolePath, cancellationToken);
        }
    }

    private static bool IsKnownWindowedExecutable(string? path) =>
        !string.IsNullOrWhiteSpace(path) &&
        string.Equals(
            Path.GetFileName(path),
            AppConstants.ConfluenceWindowedExecutableName,
            StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex("^[A-Za-z0-9._-]+$", RegexOptions.CultureInvariant)]
    internal static partial Regex SpaceKeyPattern();
}

/// <summary>Infers the instance context and optional space from Cortex-supported page URLs.</summary>
public static class ConfluencePageUrlAnalyzer
{
    private const string SpacesMarker = "/spaces/";
    private const string DisplayMarker = "/display/";
    private const string ViewPageSuffix = "/pages/viewpage.action";
    private const string TinyMarker = "/x/";

    /// <summary>Returns the base URL and inferred space without contacting Confluence.</summary>
    public static ConfluencePageUrlAnalysis Analyze(string value)
    {
        string candidate = value?.Trim() ?? string.Empty;
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw InvalidPageUrl();
        }

        // Refuse a cleartext origin here, where the user can still fix the URL,
        // rather than at the later write that Cortex would reject anyway.
        if (uri.Scheme == Uri.UriSchemeHttp &&
            !uri.IsLoopback &&
            !string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConfluenceSetupValidationException(UiStrings.ConfluenceSetupInsecurePageUrl);
        }

        string path = uri.AbsolutePath;
        int markerIndex;
        string? inferredSpace = null;
        if ((markerIndex = path.IndexOf(SpacesMarker, StringComparison.Ordinal)) >= 0)
        {
            string[] segments = path[(markerIndex + SpacesMarker.Length)..]
                .Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 3 ||
                !string.Equals(segments[1], "pages", StringComparison.Ordinal) ||
                !segments[2].All(char.IsAsciiDigit))
            {
                throw InvalidPageUrl();
            }

            inferredSpace = DecodeSpaceKey(segments[0]);
        }
        else if ((markerIndex = path.IndexOf(DisplayMarker, StringComparison.Ordinal)) >= 0)
        {
            string[] segments = path[(markerIndex + DisplayMarker.Length)..]
                .Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2)
            {
                throw InvalidPageUrl();
            }

            inferredSpace = DecodeSpaceKey(segments[0]);
        }
        else if (path.EndsWith(ViewPageSuffix, StringComparison.Ordinal))
        {
            markerIndex = path.Length - ViewPageSuffix.Length;
            string[] pageIds = uri.Query.TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split('=', 2))
                .Where(pair => pair.Length == 2 &&
                    string.Equals(Uri.UnescapeDataString(pair[0]), "pageId", StringComparison.Ordinal))
                .Select(pair => Uri.UnescapeDataString(pair[1]))
                .ToArray();
            if (pageIds.Length != 1 || pageIds[0].Length == 0 || !pageIds[0].All(char.IsAsciiDigit))
            {
                throw InvalidPageUrl();
            }
        }
        else if ((markerIndex = path.IndexOf(TinyMarker, StringComparison.Ordinal)) >= 0)
        {
            string key = path[(markerIndex + TinyMarker.Length)..];
            if (string.IsNullOrWhiteSpace(key) || key.Contains('/', StringComparison.Ordinal) || uri.Query.Length > 0)
            {
                throw InvalidPageUrl();
            }
        }
        else
        {
            throw InvalidPageUrl();
        }

        string authority = uri.GetLeftPart(UriPartial.Authority);
        string contextPath = path[..markerIndex].TrimEnd('/');
        return new ConfluencePageUrlAnalysis(authority + contextPath, inferredSpace);
    }

    private static string DecodeSpaceKey(string value)
    {
        string decoded = Uri.UnescapeDataString(value);
        if (decoded.Length == 0 || decoded.Any(character =>
            !char.IsAsciiLetterOrDigit(character) && character is not '.' and not '-' and not '_'))
        {
            throw InvalidPageUrl();
        }

        return decoded;
    }

    private static ConfluenceSetupValidationException InvalidPageUrl() =>
        new(UiStrings.ConfluenceSetupInvalidPageUrl);
}

/// <summary>Reports a localized first-run validation refusal before any write occurs.</summary>
public sealed class ConfluenceSetupValidationException : Exception
{
    /// <summary>Initializes a user-safe validation refusal.</summary>
    public ConfluenceSetupValidationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
