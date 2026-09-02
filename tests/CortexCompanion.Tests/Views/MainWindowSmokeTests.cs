// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows;
using CortexCompanion.Interfaces;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;
using CortexCompanion.ViewModels;

namespace CortexCompanion.Tests.Views;

/// <summary>Guards runtime composition of the complete WPF shell.</summary>
[TestClass]
[DoNotParallelize]
public sealed class MainWindowSmokeTests
{
    [STATestMethod]
    public void MainWindowCanBeShownWithThePendingRuntime()
    {
        using TemporaryDirectory temporary = new();
        App application = new();
        application.InitializeComponent();
        application.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        MainWindow? window = null;
        try
        {
            AppPaths paths = new(temporary.Path);
            IProcessRunner processRunner = new ProcessRunner();
            IFileDialogService fileDialogs = new FileDialogService();
            ICompanionRuntimeFactory runtimeFactory = new CompanionRuntimeFactory(
                paths,
                new CliHandshakeService(new CliVersionPolicy(), processRunner),
                processRunner,
                fileDialogs);
            ICompanionRuntimeCoordinator runtimeCoordinator = new CompanionRuntimeCoordinator(runtimeFactory);
            SettingsViewModel settings = new(
                new SettingsStore(paths.SettingsPath),
                new CliPathDiscovery(),
                runtimeCoordinator,
                new CortexConfigClient(processRunner),
                fileDialogs,
                new ConfluenceCredentialTargetProvider(),
                new WindowsCredentialManagerStore());
            MainViewModel viewModel = new(runtimeCoordinator, settings);
            window = new MainWindow(viewModel, new RunInterruptionConfirmationService())
            {
                ShowInTaskbar = false,
            };
            application.MainWindow = window;

            window.Show();

            Assert.IsTrue(window.IsVisible);
        }
        finally
        {
            window?.Close();
            application.Shutdown();
        }
    }
}
