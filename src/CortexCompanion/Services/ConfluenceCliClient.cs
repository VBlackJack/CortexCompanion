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

    /// <summary>Initializes a session-bound client with one absolute CLI path and one absolute TOML path.</summary>
    public ConfluenceCliClient(IProcessRunner processRunner, string cliPath, string configPath)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _cliPath = Path.GetFullPath(cliPath ?? throw new ArgumentNullException(nameof(cliPath)));
        _configPath = Path.GetFullPath(configPath ?? throw new ArgumentNullException(nameof(configPath)));
    }

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
        CancellationToken cancellationToken)
        where T : class
    {
        ProcessRunResult processResult = await _processRunner.RunAsync(
            new ProcessRequest(
                _cliPath,
                arguments,
                AppConstants.CliReadTimeout,
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
        PagesContract pages => pages.ContractVersion == 1 && pages.Spaces is not null && pages.LastSync is not null &&
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
        _ => false,
    };

    private static ConfluenceCliResult<T> InvalidJson<T>() =>
        new(CortexExitCode.Error, default, UiStrings.PagesCliInvalidJson, false, null);

    private static string Sanitize(string value) => value.Trim();
}
