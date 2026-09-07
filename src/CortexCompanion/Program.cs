// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;
using CortexCompanion.Services;

namespace CortexCompanion;

/// <summary>Handles process-only commands before the WPF application is initialized.</summary>
internal static class Program
{
    /// <summary>Runs a process-only command or starts the desktop application.</summary>
    [STAThread]
    public static int Main(string[] arguments)
    {
        if (arguments.Length == 1 &&
            string.Equals(
                arguments[0],
                AppConstants.CompanionVersionArgument,
                StringComparison.Ordinal))
        {
            return VersionOutputWriter.TryWriteLine(CompanionVersionProvider.GetCurrent()) ? 0 : 1;
        }

        if (arguments.Length == 1 &&
            string.Equals(
                arguments[0],
                AppConstants.CompanionUninstallCleanupArgument,
                StringComparison.Ordinal))
        {
            return UninstallCleanupCommand.RunAsync(
                    new TaskSchedulerComAdapter(),
                    Console.Out,
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        // Before the first localized string is read: several UiStrings members are static
        // formats parsed once when the type is first touched, so a later switch would leave
        // half the interface in the previous language.
        UiCultureBootstrap.Apply(new AppPaths().SettingsPath);

        App application = new();
        application.InitializeComponent();
        return application.Run();
    }
}
