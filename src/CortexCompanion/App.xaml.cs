// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows;
using System.Windows.Threading;
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

        try
        {
            SettingsStore settingsStore = new(paths.SettingsPath);
            SettingsLoadResult settingsResult = await settingsStore.LoadAsync(_applicationCancellation.Token);
            IProcessRunner processRunner = new ProcessRunner();
            CliHandshakeService handshakeService = new(
                new CliVersionPolicy(),
                processRunner);
            CliPathValidationResult cliValidation = CliPathValidator.Validate(settingsResult.Settings.CliPath);
            PagesViewModel pagesViewModel;
            if (cliValidation.IsValid && cliValidation.AbsolutePath is not null)
            {
                ConfluenceConfigPathResolution configPath = ConfluenceConfigPathResolver.Resolve(
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

            MainViewModel viewModel = new(handshakeService, pagesViewModel);
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
