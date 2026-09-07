// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CortexCompanion.Constants;
using CortexCompanion.Interfaces;
using CortexCompanion.Models;
using CortexCompanion.Services;

namespace CortexCompanion.LockProbe;

internal static class Program
{
    private const int LockedExitCode = 2;
    private const int UsageExitCode = 64;
    private const int ContractRefusedExitCode = 65;
    private const string CompatibleVersion = "2026.0808.00";
    private const string SyncProbeDelayVariable = "CORTEX_COMPANION_SYNC_PROBE_DELAY_MS";
    private const string CliSurfaceVerb = "dump-cli-arguments";
    private const int CliSurfaceContractVersion = 1;
    private const string CliSurfaceSpaceKey = "PN";
    private const string CliSurfacePageUrl = "https://wiki.test/x/AA";
    private const string CliSurfacePageId = "2134730685";
    private const string CliSurfaceQuery = "plan d'action";
    private const string CliSurfaceSection = "docs";
    private const string CliSurfaceSourceKind = "doc";
    private const string CliSurfaceExpectedHash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
    private static readonly TimeSpan CliSurfaceTimeout = TimeSpan.FromSeconds(5);

    private static async Task<int> Main(string[] arguments)
    {
        if (arguments is ["--version"])
        {
            Console.WriteLine(CompatibleVersion);
            return 0;
        }

        if (arguments is [CliSurfaceVerb])
        {
            return await DumpCliArgumentsAsync();
        }

        if (arguments is ["sync", "--json"])
        {
            // Enough data on each pipe to expose a stopped capture consumer.
            byte[] payload = Encoding.UTF8.GetBytes(new string('x', 1_000_000));
            await Console.OpenStandardOutput().WriteAsync(payload);
            await Console.OpenStandardError().WriteAsync(payload);
            return 0;
        }

        if (arguments is ["unicode-output"])
        {
            byte[] responseBytes = new UTF8Encoding(false, true).GetBytes(
                "{\"path\":\"G:/Équipe/🔒\"}");
            await Console.OpenStandardOutput().WriteAsync(responseBytes);
            return 0;
        }

        if (arguments is ["hold-process"])
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            return 0;
        }

        if (arguments is ["spawn-child-tree"])
        {
            return await RunChildTreeProbeAsync(waitForParent: true, childProcessIdPath: null);
        }

        if (arguments is ["exit-with-child-tree", string childProcessIdPath])
        {
            return await RunChildTreeProbeAsync(waitForParent: false, childProcessIdPath);
        }

        if (arguments is ["invalid-utf8"])
        {
            byte[] invalidUtf8 = [0xc3, 0x28];
            await Console.OpenStandardOutput().WriteAsync(invalidUtf8);
            return 0;
        }
        if (arguments is ["utf16-output"])
        {
            byte[] utf16Output = [0xff, 0xfe, 0x61, 0x00];
            await Console.OpenStandardOutput().WriteAsync(utf16Output);
            return 0;
        }

        if (arguments is ["confluence", "--config", _, "sync"])
        {
            return await RunSyncProbeAsync();
        }

        if (arguments.Length < 2)
        {
            Console.Error.WriteLine("Usage: CortexCompanion.LockProbe <hold|mutate> <config-path> [milliseconds]");
            return UsageExitCode;
        }

        return arguments[0] switch
        {
            "hold" => await HoldAsync(arguments),
            "mutate" => await MutateAsync(arguments[1]),
            "render-golden" => await RenderGoldenAsync(arguments),
            "validate-search" => await ValidateSearchAsync(arguments[1]),
            "validate-confluence" => arguments.Length >= 3
                ? await ValidateConfluenceAsync(arguments[1], arguments[2])
                : UsageExitCode,
            _ => UsageExitCode,
        };
    }

    private static async Task<int> ValidateSearchAsync(string path)
    {
        string payload = await File.ReadAllTextAsync(path);
        SearchClient client = new(new SearchFixtureRunner(payload), "fixture", TimeSpan.FromSeconds(5));
        SearchResponse result = await client.SearchAsync("fixture", "", "", CancellationToken.None);
        Console.WriteLine(JsonSerializer.Serialize(result));
        return 0;
    }

    private static async Task<int> ValidateConfluenceAsync(string kind, string path)
    {
        string payload = await File.ReadAllTextAsync(path);
        ConfluenceCliClient client = new(
            new SearchFixtureRunner(payload),
            Environment.ProcessPath ?? "fixture",
            path,
            TimeSpan.FromSeconds(5));
        // A verb this probe does not know is a caller mistake, not a rejected document, and
        // saying so keeps a typo from being reported as a broken contract.
        if (kind is not ("preview" or "resolve" or "pages" or "catalog" or "status"))
        {
            await Console.Error.WriteLineAsync($"Unknown contract kind: {kind}");
            return UsageExitCode;
        }

        object? value = kind switch
        {
            "preview" => (await client.PreviewAsync("https://wiki.test/x/AA", CancellationToken.None)).Value,
            "resolve" => (await client.ResolveAsync("https://wiki.test/x/AA", CancellationToken.None)).Value,
            "pages" => (await client.GetPagesAsync(CancellationToken.None)).Value,
            "catalog" => (await client.GetCatalogAsync("DOC", CancellationToken.None)).Value,
            _ => (await client.GetSourceStatusAsync(CancellationToken.None)).Value,
        };
        if (value is null)
        {
            await Console.Error.WriteLineAsync($"The {kind} document was refused by the consumer.");
            return ContractRefusedExitCode;
        }

        Console.WriteLine(JsonSerializer.Serialize(value));
        return 0;
    }

    private sealed class SearchFixtureRunner(string payload) : IProcessRunner
    {
        public Task<ProcessRunResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(ProcessRunResult.Completed(0, payload, string.Empty));
    }

    /// <summary>
    /// Writes every Cortex command line the desktop builds, each captured from the code that
    /// builds it, so the interoperability proof can parse them with the Cortex parsers.
    /// </summary>
    private static async Task<int> DumpCliArgumentsAsync()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "cortex-companion-cli-surface-" + Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
        Directory.CreateDirectory(root);
        try
        {
            // The handshake validates its path before building anything, and accepts only an
            // existing file with the Cortex executable name.
            string cliPath = Path.Combine(root, AppConstants.CliExecutableName);
            await File.WriteAllBytesAsync(cliPath, []);
            string confluenceConfig = Path.Combine(root, "confluence.toml");
            string ingestionConfig = Path.Combine(root, "ingestion.toml");
            string knowledgeBase = Path.Combine(root, "knowledge");
            List<CapturedLine> lines =
            [
                await CaptureAsync(
                    "version",
                    Inputs(),
                    runner => new CliHandshakeService(new CliVersionPolicy(), runner)
                        .EvaluateAsync(new AppSettings(cliPath))),
                await CaptureAsync(
                    "search.filtered",
                    Inputs(("query", CliSurfaceQuery), ("section", CliSurfaceSection), ("source_kind", CliSurfaceSourceKind)),
                    runner => new SearchClient(runner, cliPath, CliSurfaceTimeout)
                        .SearchAsync(CliSurfaceQuery, CliSurfaceSection, CliSurfaceSourceKind, CancellationToken.None)),
                await CaptureAsync(
                    "search.plain",
                    Inputs(("query", CliSurfaceQuery)),
                    runner => new SearchClient(runner, cliPath, CliSurfaceTimeout)
                        .SearchAsync(CliSurfaceQuery, string.Empty, string.Empty, CancellationToken.None)),
                await CaptureAsync(
                    "config.get",
                    Inputs(),
                    runner => new CortexConfigClient(runner).GetAsync(cliPath, CliSurfaceTimeout)),
                await CaptureAsync(
                    "config.set.expected_hash",
                    Inputs(("kb_path", knowledgeBase), ("expected_hash", CliSurfaceExpectedHash)),
                    runner => new CortexConfigClient(runner).SetKnowledgeBasePathAsync(
                        cliPath, knowledgeBase, CliSurfaceExpectedHash, false, CliSurfaceTimeout)),
                await CaptureAsync(
                    "config.set.expect_absent",
                    Inputs(("kb_path", knowledgeBase)),
                    runner => new CortexConfigClient(runner).SetKnowledgeBasePathAsync(
                        cliPath, knowledgeBase, null, true, CliSurfaceTimeout)),
                await CaptureAsync(
                    "confluence.catalog",
                    Inputs(("config_path", confluenceConfig), ("space_key", CliSurfaceSpaceKey)),
                    runner => ConfluenceClient(runner, cliPath, confluenceConfig)
                        .GetCatalogAsync(CliSurfaceSpaceKey, CancellationToken.None)),
                await CaptureAsync(
                    "confluence.source_status",
                    Inputs(("config_path", confluenceConfig)),
                    runner => ConfluenceClient(runner, cliPath, confluenceConfig)
                        .GetSourceStatusAsync(CancellationToken.None)),
                await CaptureAsync(
                    "confluence.pages",
                    Inputs(("config_path", confluenceConfig)),
                    runner => ConfluenceClient(runner, cliPath, confluenceConfig)
                        .GetPagesAsync(CancellationToken.None)),
                await CaptureAsync(
                    "confluence.resolve",
                    Inputs(("config_path", confluenceConfig), ("reference", CliSurfacePageUrl)),
                    runner => ConfluenceClient(runner, cliPath, confluenceConfig)
                        .ResolveAsync(CliSurfacePageUrl, CancellationToken.None)),
                await CaptureAsync(
                    "confluence.preview",
                    Inputs(("config_path", confluenceConfig), ("reference", CliSurfacePageId)),
                    runner => ConfluenceClient(runner, cliPath, confluenceConfig)
                        .PreviewAsync(CliSurfacePageId, CancellationToken.None)),
                new CapturedLine(
                    "sync.local",
                    Inputs(),
                    SyncWorkerArguments.BuildCliArguments(SyncRunKind.LocalDocuments, null, false)),
                new CapturedLine(
                    "sync.confluence",
                    Inputs(("config_path", confluenceConfig)),
                    SyncWorkerArguments.BuildCliArguments(SyncRunKind.Confluence, confluenceConfig, false)),
                new CapturedLine(
                    "sync.confluence.force",
                    Inputs(("config_path", confluenceConfig)),
                    SyncWorkerArguments.BuildCliArguments(SyncRunKind.Confluence, confluenceConfig, true)),
                new CapturedLine(
                    "scheduled.guard",
                    Inputs(("ingestion_config_path", ingestionConfig), ("source_kind", AppConstants.IngestionSourceKind)),
                    ScheduledWorkerArguments.BuildGuardArguments(ingestionConfig, AppConstants.IngestionSourceKind)),
                new CapturedLine(
                    "scheduled.sync",
                    Inputs(("confluence_config_path", confluenceConfig), ("ingestion_config_path", ingestionConfig)),
                    ScheduledWorkerArguments.BuildSyncArguments(confluenceConfig, ingestionConfig)),
            ];
            Console.WriteLine(JsonSerializer.Serialize(new CliSurfaceDocument(CliSurfaceContractVersion, lines)));
            return 0;
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static ConfluenceCliClient ConfluenceClient(IProcessRunner runner, string cliPath, string configPath) =>
        new(runner, cliPath, configPath, CliSurfaceTimeout);

    private static Dictionary<string, string> Inputs(params (string Key, string Value)[] pairs)
    {
        Dictionary<string, string> inputs = new(StringComparer.Ordinal);
        foreach ((string key, string value) in pairs)
        {
            inputs.Add(key, value);
        }

        return inputs;
    }

    private static async Task<CapturedLine> CaptureAsync(
        string name,
        Dictionary<string, string> inputs,
        Func<IProcessRunner, Task> act)
    {
        CapturingRunner runner = new();
        try
        {
            await act(runner);
        }
        catch (Exception exception) when (exception is JsonException or CortexCliContractException)
        {
            // The canned reply is no document a client accepts. Refusing it is the client's
            // job; the command line was built before the reply arrived, and that is all
            // this verb reports.
        }

        return new CapturedLine(
            name,
            inputs,
            runner.Arguments ?? throw new InvalidOperationException($"{name} built no command line."));
    }

    private sealed class CapturingRunner : IProcessRunner
    {
        public IReadOnlyList<string>? Arguments { get; private set; }

        public Task<ProcessRunResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken)
        {
            if (Arguments is not null)
            {
                throw new InvalidOperationException("One client call ran more than one process.");
            }

            Arguments = request.Arguments;
            return Task.FromResult(ProcessRunResult.Completed(0, "{}", string.Empty));
        }
    }

    private sealed record CliSurfaceDocument(
        [property: JsonPropertyName("contract_version")] int ContractVersion,
        [property: JsonPropertyName("lines")] IReadOnlyList<CapturedLine> Lines);

    private sealed record CapturedLine(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("inputs")] IReadOnlyDictionary<string, string> Inputs,
        [property: JsonPropertyName("arguments")] IReadOnlyList<string> Arguments);

    private static async Task<int> RunSyncProbeAsync()
    {
        string? rawDelay = Environment.GetEnvironmentVariable(SyncProbeDelayVariable);
        if (!int.TryParse(rawDelay, out int milliseconds) || milliseconds is < 1 or > 120_000)
        {
            Console.Error.WriteLine($"{SyncProbeDelayVariable} must be an integer from 1 to 120000.");
            return UsageExitCode;
        }

        Console.Error.WriteLine("SYNC PROBE STARTED");
        await Console.Error.FlushAsync();
        await Task.Delay(milliseconds);
        Console.Error.WriteLine("SYNC PROBE FINISHED");
        await Console.Error.FlushAsync();
        Console.Out.WriteLine("{\"published\":true,\"probe\":true}");
        await Console.Out.FlushAsync();
        return 0;
    }

    private static async Task<int> RunChildTreeProbeAsync(
        bool waitForParent,
        string? childProcessIdPath)
    {
        string? executablePath = Environment.ProcessPath;
        if (executablePath is null)
        {
            return UsageExitCode;
        }

        ProcessStartInfo startInfo = new(executablePath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("hold-process");
        using Process? child = Process.Start(startInfo);
        if (child is null)
        {
            return UsageExitCode;
        }

        Console.WriteLine(child.Id);
        await Console.Out.FlushAsync();
        if (childProcessIdPath is not null)
        {
            await File.WriteAllTextAsync(
                childProcessIdPath,
                child.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        if (waitForParent)
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
        }

        return 0;
    }

    private static async Task<int> HoldAsync(string[] arguments)
    {
        if (arguments.Length != 3 || !int.TryParse(arguments[2], out int milliseconds) || milliseconds < 1)
        {
            return UsageExitCode;
        }

        await using IAsyncDisposable mutationLock =
            await ConfluenceConfigStore.AcquireMutationLockForInteropAsync(
                arguments[1],
                CancellationToken.None);
        Console.WriteLine("C# LOCK ACQUIRED [0,1)");
        await Task.Delay(milliseconds);
        Console.WriteLine("C# LOCK RELEASED");
        return 0;
    }

    private static async Task<int> MutateAsync(string configurationPath)
    {
        ConfluenceConfigStore store = new(configurationPath);
        ConfluenceConfigSnapshot snapshot = await store.ReadAsync(CancellationToken.None);
        try
        {
            await store.WriteAsync(snapshot.Configuration, snapshot.ContentHash, CancellationToken.None);
            Console.WriteLine("C# MUTATION ACQUIRED LOCK");
            return 0;
        }
        catch (ConfluenceConfigLockedException exception)
        {
            Console.WriteLine($"C# LOCK REFUSED: {exception.Message}");
            return LockedExitCode;
        }
    }

    private static async Task<int> RenderGoldenAsync(string[] arguments)
    {
        if (arguments.Length != 3 || arguments[2] is not "v1" and not "v2" and not "v3")
        {
            return UsageExitCode;
        }

        ConfluenceConfiguration configuration = arguments[2] switch
        {
            "v1" => GoldenVersionOne(),
            "v2" => GoldenVersionTwo(),
            _ => GoldenVersionThree(),
        };
        await File.WriteAllBytesAsync(arguments[1], ConfluenceConfigRenderer.Render(configuration));
        return 0;
    }

    private static ConfluenceConfiguration GoldenVersionOne() => new(
        1,
        "https://wiki.example.test:8443/confluence",
        "l'equipe-東京",
        new DateTimeOffset(2026, 8, 5, 12, 13, 14, 123, TimeSpan.FromHours(2.5)).AddTicks(4560),
        @"C:\Program Files\Cortex\console.exe",
        1,
        1.0,
        [new ConfluenceSpaceConfiguration(
            "DOC.UNICODE", "équipe/docs", "pro-confidentiel", ConfluenceSelection.WholeSpace, [])]);

    private static ConfluenceConfiguration GoldenVersionTwo() => new(
        2,
        "https://wiki.example.test:8443/confluence",
        "l'equipe-東京",
        new DateTimeOffset(2026, 8, 5, 12, 13, 14, 123, TimeSpan.FromHours(2.5)).AddTicks(4560),
        @"C:\Program Files\Cortex\console.exe",
        1,
        0.0000001,
        [
            new ConfluenceSpaceConfiguration(
                "DOC.UNICODE", "équipe/docs", "pro-confidentiel", ConfluenceSelection.Pages, ["123", "987654321"]),
            new ConfluenceSpaceConfiguration(
                "EMPTY", "empty", "perso-non-sensible", ConfluenceSelection.Pages, []),
            new ConfluenceSpaceConfiguration(
                "ALL", "all", "pro-confidentiel", ConfluenceSelection.WholeSpace, []),
        ]);

    private static ConfluenceConfiguration GoldenVersionThree() => GoldenVersionTwo() with
    {
        SchemaVersion = 3,
        FailureThreshold = 0.1,
        Spaces =
        [
            new ConfluenceSpaceConfiguration(
                "TREE", "tree", "pro-confidentiel", ConfluenceSelection.Subtree, ["123"]),
            new ConfluenceSpaceConfiguration(
                "EMPTY", "empty", "perso-non-sensible", ConfluenceSelection.Subtree, []),
        ],
    };
}
