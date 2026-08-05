// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Tests.TestSupport;

internal sealed class TemporaryDirectory : IDisposable
{
    private const string DirectoryPrefix = "CortexCompanion.Tests.";
    private readonly string _tempRoot;

    public TemporaryDirectory()
    {
        _tempRoot = System.IO.Path.GetFullPath(System.IO.Path.GetTempPath());
        Path = System.IO.Path.Combine(_tempRoot, DirectoryPrefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string CreateFakeCli()
    {
        string executablePath = System.IO.Path.Combine(Path, "cortex.exe");
        File.WriteAllText(executablePath, "test sentinel");
        return executablePath;
    }

    public void Dispose()
    {
        string resolvedPath = System.IO.Path.GetFullPath(Path);
        string expectedPrefix = System.IO.Path.Combine(_tempRoot, DirectoryPrefix);
        if (!resolvedPath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Refusing to remove a directory outside the test prefix.");
        }

        if (Directory.Exists(resolvedPath))
        {
            Directory.Delete(resolvedPath, recursive: true);
        }
    }
}
