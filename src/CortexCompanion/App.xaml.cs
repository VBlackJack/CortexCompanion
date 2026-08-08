// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows;
using System.Windows.Threading;
using CortexCompanion.Constants;
using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Logging;
using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.ViewModels;

namespace CortexCompanion;

/// <summary>Composes the dependency-free application shell and bounded worker modes.</summary>
public partial class App : Application, IDisposable
{
    private readonly CancellationTokenSource _applicationCancellation = new();

    /// <inheritdoc />
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppPaths paths = new();
        FileLogger.Initialize(paths.LogsDirectory);
        FileLogger.Info("Cortex Companion starting");
        RegisterExceptionHandlers();

        if (await TryRunWorkerModeAsync(e.Args, paths))
        {
            return;
        }

        try
        {
            SettingsStore settingsStore = new(paths.SettingsPath);
            IProcessRunner processRunner = new ProcessRunner();
            ICliHandshakeService handshakeService = new CliHandshakeService(
                new CliVersionPolicy(),
                processRunner);
            ICompanionRuntimeFactory runtimeFactory = new CompanionRuntimeFactory(
                paths,
                handshakeService,
                processRunner);
            ICompanionRuntimeCoordinator runtimeCoordinator = new CompanionRuntimeCoordinator(runtimeFactory);
            SettingsViewModel settings = new(
                settingsStore,
                new CliPathDiscovery(),
                runtimeCoordinator,
                new CortexConfigClient(processRunner),
                new FileDialogService());
            MainViewModel viewModel = new(runtimeCoordinator, settings);
            MainWindow window = new(viewModel);
            MainWindow = window;
            window.Show();
            FileLogger.Info("Cortex Companion shell displayed");

            try
            {
                SettingsLoadResult settingsResult = await settingsStore.LoadAsync(
                    _applicationCancellation.Token);
                await viewModel.InitializeAsync(settingsResult, _applicationCancellation.Token);
                FileLogger.Info("Cortex Companion startup complete");
            }
            catch (OperationCanceledException) when (_applicationCancellation.IsCancellationRequested)
            {
                Shutdown();
            }
            catch (Exception exception)
            {
                FileLogger.Error("Cortex Companion initialization failed", exception);
                viewModel.ReportInitializationFailure();
            }
        }
        catch (Exception exception)
        {
            FileLogger.Error("Cortex Companion shell composition failed", exception);
            FileLogger.Flush();
            MessageBox.Show(
                UiStrings.FormatFatalStartupError(paths.LogsDirectory),
                UiStrings.AppTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        _applicationCancellation.Cancel();
        FileLogger.Info("Cortex Companion shutdown");
        FileLogger.Flush();
        Dispose();
        base.OnExit(e);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _applicationCancellation.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<bool> TryRunWorkerModeAsync(string[] arguments, AppPaths paths)
    {
        if (arguments.Length > 0 &&
            string.Equals(arguments[0], AppConstants.SyncWorkerArgument, StringComparison.Ordinal))
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            if (!SyncWorkerArguments.TryParse(arguments, out SyncWorkerArguments? workerArguments) ||
                workerArguments is null ||
                !SyncWorkerArguments.IsDirectChildOfRunsRoot(
                    workerArguments.RunDirectory,
                    paths.SyncRunsDirectory))
            {
                FileLogger.Warn("Detached sync worker arguments were invalid");
                Shutdown(1);
                return true;
            }

            int exitCode = await SyncWorker.ExecuteAsync(workerArguments);
            Shutdown(exitCode);
            return true;
        }

        if (arguments.Length > 0 &&
            string.Equals(arguments[0], AppConstants.ScheduledWorkerArgument, StringComparison.Ordinal))
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            if (!ScheduledWorkerArguments.TryParse(arguments, out ScheduledWorkerArguments? workerArguments) ||
                workerArguments is null ||
                !ScheduledWorkerArguments.IsExpectedRunsRoot(
                    workerArguments.RunsRoot,
                    paths.ScheduledRunsDirectory))
            {
                FileLogger.Warn("Scheduled worker arguments were invalid");
                Shutdown(1);
                return true;
            }

            IProcessRunner processRunner = new ProcessRunner();
            ICliHandshakeService handshake = new CliHandshakeService(
                new CliVersionPolicy(),
                processRunner);
            ScheduledWorker worker = new(
                handshake,
                new ScheduledProcessRunner(),
                new ScheduledRunPersistence(paths.ScheduledRunsDirectory));
            int exitCode = await worker.ExecuteAsync(workerArguments);
            Shutdown(exitCode);
            return true;
        }

        return false;
    }

    private void RegisterExceptionHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private static void OnDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs eventArgs)
    {
        FileLogger.Error("Unhandled UI exception", eventArgs.Exception);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs eventArgs)
    {
        FileLogger.Error("Unobserved task exception", eventArgs.Exception);
    }
}
