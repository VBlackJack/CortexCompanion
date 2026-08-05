// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Security.Principal;
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

/// <summary>
/// Composes the dependency-free application shell and performs the startup handshake.
/// </summary>
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

        if (e.Args.Length > 0 &&
            string.Equals(e.Args[0], AppConstants.SyncWorkerArgument, StringComparison.Ordinal))
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            if (!SyncWorkerArguments.TryParse(e.Args, out SyncWorkerArguments? workerArguments) ||
                workerArguments is null ||
                !SyncWorkerArguments.IsDirectChildOfRunsRoot(
                    workerArguments.RunDirectory,
                    paths.SyncRunsDirectory))
            {
                FileLogger.Warn("Detached sync worker arguments were invalid");
                Shutdown(1);
                return;
            }

            int workerExitCode = await SyncWorker.ExecuteAsync(workerArguments);
            Shutdown(workerExitCode);
            return;
        }

        if (e.Args.Length > 0 &&
            string.Equals(e.Args[0], AppConstants.ScheduledWorkerArgument, StringComparison.Ordinal))
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            if (!ScheduledWorkerArguments.TryParse(
                    e.Args,
                    out ScheduledWorkerArguments? scheduledArguments) ||
                scheduledArguments is null ||
                !ScheduledWorkerArguments.IsExpectedRunsRoot(
                    scheduledArguments.RunsRoot,
                    paths.ScheduledRunsDirectory))
            {
                FileLogger.Warn("Scheduled worker arguments were invalid");
                Shutdown(1);
                return;
            }

            IProcessRunner scheduledHandshakeRunner = new ProcessRunner();
            ICliHandshakeService scheduledHandshake = new CliHandshakeService(
                new CliVersionPolicy(),
                scheduledHandshakeRunner);
            ScheduledWorker scheduledWorker = new(
                scheduledHandshake,
                new ScheduledProcessRunner(),
                new ScheduledRunPersistence(paths.ScheduledRunsDirectory));
            int scheduledExitCode = await scheduledWorker.ExecuteAsync(scheduledArguments);
            Shutdown(scheduledExitCode);
            return;
        }

        try
        {
            SettingsStore settingsStore = new(paths.SettingsPath);
            SettingsLoadResult settingsResult = await settingsStore.LoadAsync(_applicationCancellation.Token);
            IProcessRunner processRunner = new ProcessRunner();
            ICliHandshakeService handshakeService = new CliHandshakeService(
                new CliVersionPolicy(),
                processRunner);
            CliPathValidationResult cliValidation = CliPathValidator.Validate(settingsResult.Settings.CliPath);
            PagesViewModel pagesViewModel;
            ConfluenceConfigPathResolution? configPath = null;
            if (cliValidation.IsValid && cliValidation.AbsolutePath is not null)
            {
                configPath = ConfluenceConfigPathResolver.Resolve(
                    cliValidation.AbsolutePath);
                IConfluenceCliClient cliClient = new ConfluenceCliClient(
                    processRunner,
                    cliValidation.AbsolutePath,
                    configPath.AbsolutePath);
                IConfluenceConfigStore configStore = new ConfluenceConfigStore(configPath.AbsolutePath);
                IPageMutationConfirmationService confirmations = new PageMutationConfirmationService();
                PagesMutationService mutations = new(cliClient, configStore, confirmations);
                pagesViewModel = new PagesViewModel(
                    cliClient,
                    mutations,
                    configPath,
                    ConfluenceEnvironmentInspector.GetActiveOverrides());
            }
            else
            {
                pagesViewModel = new PagesViewModel(
                    null,
                    null,
                    null,
                    ConfluenceEnvironmentInspector.GetActiveOverrides());
            }

            IngestionPathResolution? ingestionPath = null;
            try
            {
                ingestionPath = IngestionPathResolver.Resolve(cliValidation.AbsolutePath);
            }
            catch (IngestionPathResolutionException exception)
            {
                FileLogger.Error("Ingestion source-health path could not be resolved", exception);
            }

            ISyncRunCoordinator? syncCoordinator = null;
            IInteractiveProcessLauncher? interactiveLauncher = null;
            if (cliValidation.IsValid &&
                cliValidation.AbsolutePath is not null &&
                configPath is not null &&
                Environment.ProcessPath is not null)
            {
                syncCoordinator = new SyncRunCoordinator(paths.SyncRunsDirectory, Environment.ProcessPath);
                interactiveLauncher = new InteractiveProcessLauncher();
            }

            SyncViewModel syncViewModel = new(
                syncCoordinator,
                interactiveLauncher,
                cliValidation.AbsolutePath,
                configPath?.AbsolutePath,
                ingestionPath,
                ConfluenceEnvironmentInspector.GetActiveOverrides());
            ScheduledTaskContract? scheduledTaskContract = null;
            if (cliValidation.IsValid &&
                cliValidation.AbsolutePath is not null &&
                configPath is not null &&
                ingestionPath is not null &&
                Environment.ProcessPath is not null &&
                File.Exists(configPath.AbsolutePath))
            {
                scheduledTaskContract = new ScheduledTaskContract(
                    Path.GetFullPath(Environment.ProcessPath),
                    cliValidation.AbsolutePath,
                    ingestionPath.ConfigPath,
                    configPath.AbsolutePath,
                    paths.ScheduledRunsDirectory,
                    AppConstants.IngestionSourceKind,
                    WindowsIdentity.GetCurrent().Name);
            }

            SchedulingViewModel schedulingViewModel = new(
                new TaskSchedulerComAdapter(),
                new SchedulingConfirmationService(),
                new ScheduledRunPersistence(paths.ScheduledRunsDirectory),
                scheduledTaskContract,
                SchedulingEnvironmentInspector.GetActiveVariableNames());
            MainViewModel viewModel = new(
                handshakeService,
                pagesViewModel,
                syncViewModel,
                schedulingViewModel);
            await viewModel.InitializeAsync(settingsResult.Settings, _applicationCancellation.Token);

            MainWindow window = new(viewModel);
            MainWindow = window;
            window.Show();
            FileLogger.Info("Cortex Companion startup complete");
        }
        catch (OperationCanceledException) when (_applicationCancellation.IsCancellationRequested)
        {
            Shutdown();
        }
        catch (Exception exception)
        {
            FileLogger.Error("Cortex Companion startup failed", exception);
            MessageBox.Show(
                UiStrings.FatalStartupError,
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
