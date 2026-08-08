// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Localization;

namespace CortexCompanion.Tests.Views;

[TestClass]
public sealed class StartupRecoveryContractTests
{
    [TestMethod]
    public void FatalMessageNamesTheExactLogDirectoryWithoutClaimingAWriteSucceeded()
    {
        const string LogDirectory = @"C:\Users\Example\AppData\Local\CortexCompanion\logs";

        string message = UiStrings.FormatFatalStartupError(LogDirectory);

        Assert.Contains(LogDirectory, message, StringComparison.Ordinal);
        Assert.Contains("s'ils sont disponibles", message, StringComparison.Ordinal);
        Assert.DoesNotContain("a été écrit", message, StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public void FatalStartupFlushesAfterLoggingAndBeforeDialogAndShutdown()
    {
        string source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CortexCompanion",
            "App.xaml.cs"));
        int fatalLog = source.IndexOf(
            "FileLogger.Error(\"Cortex Companion shell composition failed\"",
            StringComparison.Ordinal);
        int flush = source.IndexOf("FileLogger.Flush();", fatalLog, StringComparison.Ordinal);
        int dialog = source.IndexOf("MessageBox.Show(", flush, StringComparison.Ordinal);
        int shutdown = source.IndexOf("Shutdown();", dialog, StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, fatalLog);
        Assert.IsGreaterThan(fatalLog, flush);
        Assert.IsGreaterThan(flush, dialog);
        Assert.IsGreaterThan(dialog, shutdown);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located.");
    }
}
