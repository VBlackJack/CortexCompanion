// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text.Json;
using CortexCompanion.Constants;
using CortexCompanion.Interfaces;
using CortexCompanion.Models;

namespace CortexCompanion.Services;

/// <summary>Implements the Cortex configuration JSON contract without reading TOML files.</summary>
public sealed class CortexConfigClient : ICortexConfigClient
{
    private readonly IProcessRunner _processRunner;

    /// <summary>Initializes the client with a bounded process boundary.</summary>
    public CortexConfigClient(IProcessRunner processRunner)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
    }

    /// <inheritdoc />
    public async Task<CortexConfigSnapshot> GetAsync(
        string cliPath,
        CancellationToken cancellationToken = default)
    {
        ProcessRunResult processResult = await RunAsync(
            cliPath,
            ["config", "get", "--json"],
            cancellationToken);
        if (processResult.ExitCode != 0)
        {
            throw new CortexCliContractException("Cortex config get returned a nonzero exit code.");
        }

        using JsonDocument document = ParseDocument(processResult.StandardOutput);
        JsonElement root = document.RootElement;
        ValidateEnvelope(root, "config_get");
        RequireObjectWithExactProperties(
            root,
            [
                "contract_version",
                "operation",
                "status",
                "present",
                "content_hash",
                "valid",
                "error",
                "values",
                "restart_required",
                "reindex_required",
            ]);
        string status = ReadRequiredString(root, "status");
        if (!string.Equals(status, "succeeded", StringComparison.Ordinal))
        {
            throw new CortexCliContractException("Cortex config get returned an unsupported status.");
        }

        bool present = ReadRequiredBoolean(root, "present");
        bool valid = ReadRequiredBoolean(root, "valid");
        string? contentHash = ReadNullableString(root, "content_hash");
        CortexCliError? error = ReadError(root);
        JsonElement values = ReadRequiredProperty(root, "values");
        bool restartRequired = ReadRequiredBoolean(root, "restart_required");
        bool reindexRequired = ReadRequiredBoolean(root, "reindex_required");
        if (values.ValueKind == JsonValueKind.Object)
        {
            ValidateConfigValues(values);
        }

        string? knowledgeBasePath = values.ValueKind == JsonValueKind.Null
            ? null
            : ReadNullableString(values, "kb_path");

        if (valid != (values.ValueKind == JsonValueKind.Object) ||
            valid != (error is null) ||
            restartRequired ||
            reindexRequired ||
            (contentHash is not null && !IsLowercaseSha256(contentHash)))
        {
            throw new CortexCliContractException("Cortex config get returned an inconsistent payload.");
        }

        return new CortexConfigSnapshot(present, contentHash, valid, knowledgeBasePath, error);
    }

    /// <inheritdoc />
    public async Task<CortexConfigMutationResult> SetKnowledgeBasePathAsync(
        string cliPath,
        string knowledgeBasePath,
        string? expectedContentHash,
        bool expectAbsent,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(knowledgeBasePath);
        if (expectAbsent == (expectedContentHash is not null))
        {
            throw new ArgumentException(
                "Exactly one compare-and-swap precondition must be supplied.",
                nameof(expectedContentHash));
        }

        List<string> arguments = ["config", "set", "--json"];
        if (expectAbsent)
        {
            arguments.Add("--expect-absent");
        }
        else
        {
            arguments.Add("--expected-hash");
            string requiredContentHash = expectedContentHash
                ?? throw new InvalidOperationException("The content hash precondition is missing.");
            arguments.Add(requiredContentHash);
        }

        arguments.Add("--kb-path");
        arguments.Add(knowledgeBasePath);
        ProcessRunResult processResult = await RunAsync(cliPath, arguments, cancellationToken);
        using JsonDocument document = ParseDocument(processResult.StandardOutput);
        JsonElement root = document.RootElement;
        ValidateEnvelope(root, "config_set");
        RequireObjectWithExactProperties(
            root,
            [
                "contract_version",
                "operation",
                "status",
                "changed",
                "previous_content_hash",
                "content_hash",
                "backup_written",
                "rebuilt_from_defaults",
                "restart_required",
                "reindex_required",
                "error",
            ]);
        string rawStatus = ReadRequiredString(root, "status");
        CortexConfigMutationStatus status = rawStatus switch
        {
            "succeeded" => CortexConfigMutationStatus.Succeeded,
            "unchanged" => CortexConfigMutationStatus.Unchanged,
            "conflict" => CortexConfigMutationStatus.Conflict,
            "locked" => CortexConfigMutationStatus.Locked,
            "failed" => CortexConfigMutationStatus.Failed,
            _ => throw new CortexCliContractException("Cortex config set returned an unsupported status."),
        };

        bool changed = ReadRequiredBoolean(root, "changed");
        string? previousContentHash = ReadNullableString(root, "previous_content_hash");
        string? contentHash = ReadNullableString(root, "content_hash");
        bool backupWritten = ReadRequiredBoolean(root, "backup_written");
        bool rebuiltFromDefaults = ReadRequiredBoolean(root, "rebuilt_from_defaults");
        bool restartRequired = ReadRequiredBoolean(root, "restart_required");
        bool reindexRequired = ReadRequiredBoolean(root, "reindex_required");
        CortexCliError? error = ReadError(root);
        if (!ExitCodeMatches(status, error, processResult.ExitCode) ||
            !MutationEnvelopeMatches(
                status,
                changed,
                previousContentHash,
                contentHash,
                backupWritten,
                rebuiltFromDefaults,
                restartRequired,
                reindexRequired,
                error) ||
            (previousContentHash is not null && !IsLowercaseSha256(previousContentHash)) ||
            (contentHash is not null && !IsLowercaseSha256(contentHash)))
        {
            throw new CortexCliContractException("Cortex config set returned an incoherent result envelope.");
        }

        return new CortexConfigMutationResult(
            status,
            changed,
            contentHash,
            restartRequired,
            reindexRequired,
            error);
    }

    private async Task<ProcessRunResult> RunAsync(
        string cliPath,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cliPath);
        ProcessRequest request = new(
            cliPath,
            arguments,
            AppConstants.CliConfigurationTimeout,
            AppConstants.MaxProcessOutputCharacters);
        ProcessRunResult result = await _processRunner.RunAsync(request, cancellationToken);
        if (result.OutcomeUnknown)
        {
            throw new CortexCliContractException(
                "Cortex configuration operation ended without a trustworthy outcome.",
                outcomeUnknown: true);
        }

        if (result.LaunchError is not null || result.ExitCode is null)
        {
            throw new CortexCliContractException("Cortex configuration operation could not be started.");
        }

        return result;
    }

    private static JsonDocument ParseDocument(string json)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new CortexCliContractException("Cortex returned invalid configuration JSON.", exception);
        }
    }

    private static void ValidateEnvelope(JsonElement root, string expectedOperation)
    {
        if (root.ValueKind != JsonValueKind.Object ||
            ReadRequiredInt32(root, "contract_version") != AppConstants.ConfigContractVersion ||
            !string.Equals(ReadRequiredString(root, "operation"), expectedOperation, StringComparison.Ordinal))
        {
            throw new CortexCliContractException("Cortex returned an incompatible configuration contract.");
        }
    }

    private static CortexCliError? ReadError(JsonElement root)
    {
        JsonElement error = ReadRequiredProperty(root, "error");
        if (error.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (error.ValueKind != JsonValueKind.Object)
        {
            throw new CortexCliContractException("Cortex returned an invalid structured error.");
        }

        RequireObjectWithExactProperties(error, ["code", "phase", "path"]);
        JsonElement path = ReadRequiredProperty(error, "path");
        if (path.ValueKind != JsonValueKind.Null)
        {
            throw new CortexCliContractException("Cortex configuration error disclosed a path.");
        }

        string code = ReadRequiredString(error, "code");
        if (code is not (
            "invalid_configuration" or
            "invalid_argument" or
            "hash_mismatch" or
            "locked" or
            "write_failed" or
            "validation_failed"))
        {
            throw new CortexCliContractException("Cortex returned an unsupported configuration error code.");
        }

        return new CortexCliError(code, ReadRequiredIdentifier(error, "phase"));
    }

    private static bool ExitCodeMatches(
        CortexConfigMutationStatus status,
        CortexCliError? error,
        int? exitCode)
    {
        if (status is CortexConfigMutationStatus.Succeeded or CortexConfigMutationStatus.Unchanged)
        {
            return error is null && exitCode == AppConstants.CliExitSuccess;
        }

        if (error is null)
        {
            return false;
        }

        int expectedExitCode = error.Code switch
        {
            "hash_mismatch" => AppConstants.CliExitConflict,
            "locked" => AppConstants.CliExitLocked,
            "invalid_argument" => AppConstants.CliExitInvalidInput,
            "invalid_configuration" or "write_failed" or "validation_failed" =>
                AppConstants.CliExitError,
            _ => -1,
        };
        CortexConfigMutationStatus expectedStatus = error.Code switch
        {
            "hash_mismatch" => CortexConfigMutationStatus.Conflict,
            "locked" => CortexConfigMutationStatus.Locked,
            _ => CortexConfigMutationStatus.Failed,
        };
        return status == expectedStatus && exitCode == expectedExitCode;
    }

    private static bool MutationEnvelopeMatches(
        CortexConfigMutationStatus status,
        bool changed,
        string? previousContentHash,
        string? contentHash,
        bool backupWritten,
        bool rebuiltFromDefaults,
        bool restartRequired,
        bool reindexRequired,
        CortexCliError? error) => status switch
        {
            CortexConfigMutationStatus.Succeeded =>
                changed &&
                contentHash is not null &&
                !string.Equals(previousContentHash, contentHash, StringComparison.Ordinal) &&
                backupWritten == (previousContentHash is not null) &&
                (!rebuiltFromDefaults || previousContentHash is not null) &&
                restartRequired &&
                error is null,
            CortexConfigMutationStatus.Unchanged =>
                !changed && previousContentHash is not null &&
                string.Equals(previousContentHash, contentHash, StringComparison.Ordinal) &&
                !backupWritten && !rebuiltFromDefaults && !restartRequired && !reindexRequired && error is null,
            CortexConfigMutationStatus.Conflict or
            CortexConfigMutationStatus.Locked or
            CortexConfigMutationStatus.Failed =>
                !changed &&
                string.Equals(previousContentHash, contentHash, StringComparison.Ordinal) &&
                !backupWritten && !rebuiltFromDefaults && !restartRequired && !reindexRequired && error is not null,
            _ => false,
        };

    private static void ValidateConfigValues(JsonElement values)
    {
        RequireObjectWithExactProperties(
            values,
            [
                "schema_version",
                "kb_path",
                "chroma_path",
                "index_whole_folder",
                "included_sections",
                "excluded_dirs",
                "exclude_files",
                "max_markdown_file_size_bytes",
                "max_pdf_size_bytes",
                "write_lock_path",
                "write_lock_timeout_seconds",
            ]);
        if (ReadRequiredInt32(values, "schema_version") != 1)
        {
            throw new CortexCliContractException(
                "Cortex configuration values used an unsupported schema version.");
        }
        _ = ReadNullableString(values, "kb_path");
        _ = ReadRequiredString(values, "chroma_path");
        _ = ReadRequiredBoolean(values, "index_whole_folder");
        ValidateStringArray(values, "included_sections");
        ValidateStringArray(values, "excluded_dirs");
        ValidateStringArray(values, "exclude_files");
        _ = ReadRequiredInt32(values, "max_markdown_file_size_bytes");
        _ = ReadRequiredInt32(values, "max_pdf_size_bytes");
        _ = ReadRequiredString(values, "write_lock_path");
        JsonElement timeout = ReadRequiredProperty(values, "write_lock_timeout_seconds");
        if (timeout.ValueKind != JsonValueKind.Number || !timeout.TryGetDouble(out _))
        {
            throw new CortexCliContractException(
                "Cortex JSON field 'write_lock_timeout_seconds' was not numeric.");
        }
    }

    private static void ValidateStringArray(JsonElement element, string name)
    {
        JsonElement values = ReadRequiredProperty(element, name);
        if (values.ValueKind != JsonValueKind.Array ||
            values.EnumerateArray().Any(value => value.ValueKind != JsonValueKind.String))
        {
            throw new CortexCliContractException($"Cortex JSON field '{name}' was not a text array.");
        }
    }

    private static void RequireObjectWithExactProperties(
        JsonElement element,
        IReadOnlyCollection<string> expectedNames)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new CortexCliContractException("Cortex JSON contained an invalid object.");
        }

        HashSet<string> actualNames = new(StringComparer.Ordinal);
        foreach (JsonProperty property in element.EnumerateObject())
        {
            actualNames.Add(property.Name);
        }

        if (!actualNames.SetEquals(expectedNames))
        {
            throw new CortexCliContractException("Cortex JSON object fields did not match the contract.");
        }
    }

    private static bool IsLowercaseSha256(string value)
    {
        return value.Length == 64 && value.All(character =>
            character is >= '0' and <= '9' or >= 'a' and <= 'f');
    }

    private static JsonElement ReadRequiredProperty(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement value))
        {
            throw new CortexCliContractException($"Cortex JSON omitted required field '{name}'.");
        }

        return value;
    }

    private static string ReadRequiredString(JsonElement element, string name)
    {
        JsonElement value = ReadRequiredProperty(element, name);
        if (value.ValueKind != JsonValueKind.String)
        {
            throw new CortexCliContractException($"Cortex JSON field '{name}' was not a string.");
        }

        return value.GetString()
            ?? throw new CortexCliContractException($"Cortex JSON field '{name}' was null.");
    }

    private static string ReadRequiredIdentifier(JsonElement element, string name)
    {
        string value = ReadRequiredString(element, name);
        if (value.Length is < 1 or > 64 ||
            value[0] is not (>= 'a' and <= 'z') ||
            value.Any(character => character is not (>= 'a' and <= 'z' or >= '0' and <= '9' or '_')))
        {
            throw new CortexCliContractException($"Cortex JSON field '{name}' was not a safe identifier.");
        }

        return value;
    }

    private static string? ReadNullableString(JsonElement element, string name)
    {
        JsonElement value = ReadRequiredProperty(element, name);
        if (value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw new CortexCliContractException($"Cortex JSON field '{name}' was not nullable text.");
        }

        return value.GetString();
    }

    private static bool ReadRequiredBoolean(JsonElement element, string name)
    {
        JsonElement value = ReadRequiredProperty(element, name);
        if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            throw new CortexCliContractException($"Cortex JSON field '{name}' was not Boolean.");
        }

        return value.GetBoolean();
    }

    private static int ReadRequiredInt32(JsonElement element, string name)
    {
        JsonElement value = ReadRequiredProperty(element, name);
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int result))
        {
            throw new CortexCliContractException($"Cortex JSON field '{name}' was not an integer.");
        }

        return result;
    }
}
