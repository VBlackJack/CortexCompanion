// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;
using System.Text;
using CortexCompanion.Models;
using CortexCompanion.Services;

namespace CortexCompanion.LockProbe;

internal static class Program
{
    private const int LockedExitCode = 2;
    private const int UsageExitCode = 64;
    private const string CompatibleVersion = "2026.0808.00";
    private const string SyncProbeDelayVariable = "CORTEX_COMPANION_SYNC_PROBE_DELAY_MS";

    private static async Task<int> Main(string[] arguments)
    {
        if (arguments is ["--version"])
        {
            Console.WriteLine(CompatibleVersion);
            return 0;
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
            _ => UsageExitCode,
        };
    }

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
