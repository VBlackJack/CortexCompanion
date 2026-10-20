// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class BestEffortFileTests
{
    [TestMethod]
    public void ALockedTemporaryFileNeverMasksTheProtectedResult()
    {
        using TemporaryDirectory temporary = new();
        string path = Path.Combine(temporary.Path, ".settings.json.tmp");
        using (FileStream held = new(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
        {
            BestEffortFile.Delete(path);

            Assert.IsTrue(File.Exists(path), "A file held open cannot be deleted; the helper must leave it and continue.");
        }

        BestEffortFile.Delete(path);
        BestEffortFile.Delete(path);
        BestEffortFile.Delete(null);
        BestEffortFile.Delete(Path.Combine(temporary.Path, "missing-directory", "file.tmp"));

        Assert.IsFalse(File.Exists(path));
    }
}
