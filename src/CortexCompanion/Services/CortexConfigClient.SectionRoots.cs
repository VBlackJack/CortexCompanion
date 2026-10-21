// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text.Json;
using CortexCompanion.Constants;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Implements the section-root JSON contract on the same bounded process boundary.</summary>
public sealed partial class CortexConfigClient
{
    private const string RootsOperation = "config_roots";
    private const string SetRootOperation = "config_set_root";
    private const string ClearRootOperation = "config_clear_root";
    private const string SucceededStatus = "succeeded";

    private static readonly JsonSerializerOptions SectionRootJsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        RespectNullableAnnotations = true,
    };

    /// <inheritdoc />
    public async Task<SectionRootsSnapshot> GetSectionRootsAsync(
        string cliPath,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ProcessRunResult processResult = await RunAsync(
            cliPath,
            ["config", "roots", "--json"],
            timeout,
            cancellationToken);
        if (processResult.ExitCode != AppConstants.CliExitSuccess)
        {
            throw new CortexCliContractException("Cortex config roots returned a nonzero exit code.");
        }

        SectionRootsContract contract = DeserializeSectionRootDocument<SectionRootsContract>(processResult.StandardOutput);
        if (contract.ContractVersion != AppConstants.ConfigContractVersion ||
            !string.Equals(contract.Operation, RootsOperation, StringComparison.Ordinal) ||
            !string.Equals(contract.Status, SucceededStatus, StringComparison.Ordinal))
        {
            throw new CortexCliContractException("Cortex returned an incompatible section-root contract.");
        }

        CortexCliError? error = ProjectSectionRootError(contract.Error);
        bool consistent = contract.Valid
            ? error is null && contract.IndexWholeFolder is not null
            : error is not null && contract.KnowledgeBasePath is null &&
              contract.IndexWholeFolder is null && contract.Sections.Count == 0;
        if (!consistent ||
            (contract.ContentHash is not null && !IsLowercaseSha256(contract.ContentHash)) ||
            contract.Sections.Any(entry => string.IsNullOrWhiteSpace(entry.Section) ||
                (entry.Root is null && (entry.Mounted || entry.Exists))) ||
            contract.Sections.Select(entry => entry.Section).Distinct(StringComparer.Ordinal).Count() !=
                contract.Sections.Count)
        {
            throw new CortexCliContractException("Cortex config roots returned an inconsistent payload.");
        }

        return new SectionRootsSnapshot(
            contract.Present,
            contract.ContentHash,
            contract.Valid,
            contract.KnowledgeBasePath,
            contract.IndexWholeFolder == true,
            [.. contract.Sections.Select(entry => new SectionRoot(entry.Section, entry.Root, entry.Mounted, entry.Exists))],
            error);
    }

    /// <inheritdoc />
    public Task<SectionRootMutationResult> SetSectionRootAsync(
        string cliPath,
        string section,
        string root,
        string expectedContentHash,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        return MutateSectionRootAsync(
            cliPath,
            SetRootOperation,
            section,
            ["config", "set-root", section, root, "--json", "--expected-hash", expectedContentHash],
            timeout,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<SectionRootMutationResult> ClearSectionRootAsync(
        string cliPath,
        string section,
        string expectedContentHash,
        TimeSpan timeout,
        CancellationToken cancellationToken = default) =>
        MutateSectionRootAsync(
            cliPath,
            ClearRootOperation,
            section,
            ["config", "clear-root", section, "--json", "--expected-hash", expectedContentHash],
            timeout,
            cancellationToken);

    private async Task<SectionRootMutationResult> MutateSectionRootAsync(
        string cliPath,
        string operation,
        string section,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(section);
        if (arguments.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Every section-root argument must be supplied.", nameof(arguments));
        }

        ProcessRunResult processResult = await RunAsync(cliPath, arguments, timeout, cancellationToken);
        SectionRootMutationContract contract =
            DeserializeSectionRootDocument<SectionRootMutationContract>(processResult.StandardOutput);
        if (contract.ContractVersion != AppConstants.ConfigContractVersion ||
            !string.Equals(contract.Operation, operation, StringComparison.Ordinal) ||
            !string.Equals(contract.Section, section, StringComparison.Ordinal))
        {
            throw new CortexCliContractException("Cortex returned an incompatible section-root contract.");
        }

        CortexConfigMutationStatus status = contract.Status switch
        {
            "succeeded" => CortexConfigMutationStatus.Succeeded,
            "unchanged" => CortexConfigMutationStatus.Unchanged,
            "conflict" => CortexConfigMutationStatus.Conflict,
            "locked" => CortexConfigMutationStatus.Locked,
            "failed" => CortexConfigMutationStatus.Failed,
            _ => throw new CortexCliContractException("Cortex section-root mutation returned an unsupported status."),
        };
        CortexCliError? error = ProjectSectionRootError(contract.Error);
        bool mounting = string.Equals(operation, SetRootOperation, StringComparison.Ordinal);
        if (!ExitCodeMatches(status, error, processResult.ExitCode) ||
            !SectionRootEnvelopeMatches(status, mounting, contract, error) ||
            (contract.PreviousContentHash is not null && !IsLowercaseSha256(contract.PreviousContentHash)) ||
            (contract.ContentHash is not null && !IsLowercaseSha256(contract.ContentHash)))
        {
            throw new CortexCliContractException("Cortex section-root mutation returned an incoherent result envelope.");
        }

        return new SectionRootMutationResult(
            status,
            contract.Changed,
            contract.Root,
            contract.ContentHash,
            contract.RestartRequired,
            error);
    }

    private static bool SectionRootEnvelopeMatches(
        CortexConfigMutationStatus status,
        bool mounting,
        SectionRootMutationContract contract,
        CortexCliError? error)
    {
        bool sameHash = string.Equals(contract.PreviousContentHash, contract.ContentHash, StringComparison.Ordinal);
        bool rootMatches = mounting == (contract.Root is not null);
        return status switch
        {
            CortexConfigMutationStatus.Succeeded =>
                contract.Changed && contract.ContentHash is not null && contract.PreviousContentHash is not null &&
                !sameHash && contract.BackupWritten && contract.RestartRequired && rootMatches && error is null,
            CortexConfigMutationStatus.Unchanged =>
                !contract.Changed && contract.PreviousContentHash is not null && sameHash &&
                !contract.BackupWritten && !contract.RestartRequired && rootMatches && error is null,
            CortexConfigMutationStatus.Conflict or
            CortexConfigMutationStatus.Locked or
            CortexConfigMutationStatus.Failed =>
                !contract.Changed && sameHash && !contract.BackupWritten && !contract.RestartRequired &&
                contract.Root is null && error is not null,
            _ => false,
        };
    }

    private static T DeserializeSectionRootDocument<T>(string json)
        where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, SectionRootJsonOptions)
                ?? throw new CortexCliContractException("Cortex returned an empty section-root document.");
        }
        catch (JsonException exception)
        {
            throw new CortexCliContractException("Cortex returned an invalid section-root document.", exception);
        }
    }

    private static CortexCliError? ProjectSectionRootError(SectionRootErrorContract? error)
    {
        if (error is null)
        {
            return null;
        }

        if (error.Path is not null)
        {
            throw new CortexCliContractException("Cortex configuration error disclosed a path.");
        }

        if (error.Code is not (
            "invalid_configuration" or
            "invalid_argument" or
            "hash_mismatch" or
            "locked" or
            "write_failed" or
            "validation_failed"))
        {
            throw new CortexCliContractException("Cortex returned an unsupported configuration error code.");
        }

        if (!IsSafeIdentifier(error.Phase))
        {
            throw new CortexCliContractException("Cortex JSON field 'phase' was not a safe identifier.");
        }

        return new CortexCliError(error.Code, error.Phase);
    }
}
