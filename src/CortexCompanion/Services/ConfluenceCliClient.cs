// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text.Json;
using CortexCompanion.Constants;
using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Invokes only the frozen Cortex Confluence JSON commands through the configured child process.</summary>
public sealed class ConfluenceCliClient : IConfluenceCliClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
    };
    private readonly IProcessRunner _processRunner;
    private readonly string _cliPath;
    private readonly string _configPath;
    private readonly TimeSpan _timeout;

    /// <summary>Initializes a session-bound client with absolute paths and the validated shared timeout.</summary>
    public ConfluenceCliClient(
        IProcessRunner processRunner,
        string cliPath,
        string configPath,
        TimeSpan timeout)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _cliPath = Path.GetFullPath(cliPath ?? throw new ArgumentNullException(nameof(cliPath)));
        _configPath = Path.GetFullPath(configPath ?? throw new ArgumentNullException(nameof(configPath)));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        _timeout = timeout;
    }

    /// <inheritdoc />
    public Task<ConfluenceCliResult<SourceCatalogContract>> GetCatalogAsync(string spaceKey, CancellationToken cancellationToken) =>
        RunAsync<SourceCatalogContract>(
            ["confluence", "--config", _configPath, "catalog", spaceKey, "--json"],
            cancellationToken,
            _timeout > AppConstants.MinimumCatalogTimeout ? _timeout : AppConstants.MinimumCatalogTimeout);

    /// <inheritdoc />
    public Task<ConfluenceCliResult<SourceStatusContract>> GetSourceStatusAsync(CancellationToken cancellationToken) =>
        RunAsync<SourceStatusContract>(["confluence", "--config", _configPath, "source-status", "--json"], cancellationToken);

    /// <inheritdoc />
    public Task<ConfluenceCliResult<PagesContract>> GetPagesAsync(CancellationToken cancellationToken) =>
        RunAsync<PagesContract>(["confluence", "--config", _configPath, "pages", "--json"], cancellationToken);

    /// <inheritdoc />
    public Task<ConfluenceCliResult<ResolvedPageContract>> ResolveAsync(
        string reference,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        return RunAsync<ResolvedPageContract>(
            ["confluence", "--config", _configPath, "resolve", reference.Trim(), "--json"],
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<ConfluenceCliResult<ScopePreviewContract>> PreviewAsync(
        string reference,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        return RunAsync<ScopePreviewContract>(
            ["confluence", "--config", _configPath, "preview", reference.Trim(), "--json"],
            cancellationToken);
    }

    /// <summary>Maps the complete frozen numeric process contract, failing unknown values to the generic error.</summary>
    public static CortexExitCode MapExitCode(int? exitCode) => exitCode switch
    {
        0 => CortexExitCode.Ok,
        1 => CortexExitCode.Error,
        2 => CortexExitCode.Locked,
        3 => CortexExitCode.NotDue,
        4 => CortexExitCode.Auth,
        5 => CortexExitCode.Remote,
        6 => CortexExitCode.InvalidInput,
        7 => CortexExitCode.NotFound,
        8 => CortexExitCode.OutsideAllowlist,
        _ => CortexExitCode.Error,
    };

    private async Task<ConfluenceCliResult<T>> RunAsync<T>(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken,
        TimeSpan? timeout = null)
        where T : class
    {
        ProcessRunResult processResult = await _processRunner.RunAsync(
            new ProcessRequest(
                _cliPath,
                arguments,
                timeout ?? _timeout,
                AppConstants.MaxProcessOutputCharacters),
            cancellationToken);
        CortexExitCode exitCode = MapExitCode(processResult.ExitCode);
        if (processResult.TimedOut || processResult.LaunchError is not null || exitCode != CortexExitCode.Ok)
        {
            return new ConfluenceCliResult<T>(
                exitCode,
                null,
                Sanitize(processResult.StandardError),
                processResult.TimedOut,
                processResult.LaunchError);
        }

        try
        {
            T? value = JsonSerializer.Deserialize<T>(processResult.StandardOutput, JsonOptions);
            if (value is null || !HasValidContract(value))
            {
                return InvalidJson<T>();
            }

            return new ConfluenceCliResult<T>(exitCode, value, string.Empty, false, null);
        }
        catch (JsonException)
        {
            return InvalidJson<T>();
        }
    }

    private static bool HasValidContract<T>(T value) => value switch
    {
        SourceStatusContract status => status.ContractVersion == 1 && status.Status is null or "ok" or "degraded" or "error",
        SourceCatalogContract catalog => catalog.ContractVersion == 1 && !string.IsNullOrWhiteSpace(catalog.SpaceKey) &&
            catalog.Pages is not null && catalog.Pages.All(page => page is not null && !string.IsNullOrWhiteSpace(page.Title) &&
                IsNumeric(page.PageId) && page.AncestorIds is not null && page.AncestorIds.All(IsNumeric) &&
                !page.AncestorIds.Contains(page.PageId) && page.AncestorIds.Distinct().Count() == page.AncestorIds.Count) &&
            catalog.Pages.Select(page => page.PageId).Distinct().Count() == catalog.Pages.Count,
        PagesContract pages => pages.ContractVersion == 2 && pages.Spaces is not null && pages.LastSync is not null &&
            pages.Spaces.All(space =>
                space.SpaceKey is not null && space.Target is not null &&
                space.Classification is "perso-non-sensible" or "pro-confidentiel" &&
                (space.Selection == "whole_space"
                    ? space.Pages is null
                    : space.Selection is "pages" or "subtree" && space.Pages is not null &&
                      space.Pages.All(page => page.PageId is not null))) &&
            pages.LastSync.Status is null or "ok" or "degraded" or "error",
        ResolvedPageContract resolved => resolved.ContractVersion == 1 &&
            resolved.PageId is not null && resolved.Title is not null && resolved.SpaceKey is not null,
        ScopePreviewContract preview => preview.ContractVersion == 1 &&
            preview.PageId is not null && preview.Title is not null && preview.SpaceKey is not null &&
            preview.StorageRoot is not null && preview.RetentionGenerations >= 1 &&
            preview.RecommendedSelection is "pages" or "subtree" &&
            HasValidChoice(preview.PageOnly) && HasValidChoice(preview.Subtree) &&
            HasValidChoice(preview.WholeSpace),
        _ => false,
    };

    private static bool IsNumeric(string? value) => !string.IsNullOrEmpty(value) && value.All(char.IsAsciiDigit);

    private static bool HasValidChoice(ScopeChoiceContract choice) =>
        choice is not null && choice.PageCount >= 0 && choice.EstimatedBytes >= 0;

    private static ConfluenceCliResult<T> InvalidJson<T>() =>
        new(CortexExitCode.Error, default, UiStrings.PagesCliInvalidJson, false, null);

    private static string Sanitize(string value) => value.Trim();
}
