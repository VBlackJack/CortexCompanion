// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Models;
using CortexCompanion.Services;
using CortexCompanion.Tests.TestSupport;
using CortexCompanion.ViewModels;
using CortexCompanion.Views;

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
            MainViewModel viewModel = new(runtimeCoordinator, settings, new HistoryViewModel(new OperationHistoryReader(paths)));
            window = new MainWindow(viewModel, new RunInterruptionConfirmationService())
            {
                ShowInTaskbar = false,
            };
            application.MainWindow = window;

            window.Show();

            Assert.IsTrue(window.IsVisible);
            window.Width = window.MinWidth;
            window.Height = window.MinHeight;
            window.UpdateLayout();
            Capture(window, "home");
            Assert.IsTrue(viewModel.IsHomeVisible);
            HomeView home = Descendants(window).OfType<HomeView>().Single();
            Button next = Descendants(home).OfType<Button>().Single(button => ReferenceEquals(button.Command, viewModel.ContinueSetupCommand));
            Assert.IsTrue(next.IsEnabled);
            next.Command.Execute(next.CommandParameter);
            Assert.IsTrue(viewModel.IsSettingsVisible);
            viewModel.NavigateCommand.Execute(NavigationPage.Search);
            Assert.IsTrue(viewModel.IsSearchVisible);
            viewModel.Search.Results.Add(new SearchHit("fixture", "Document de validation graphique",
                string.Join(" ", Enumerable.Repeat("Extrait lisible avec accents et contenu long.", 30)),
                "operations/document.md", "operations", "note", "2026-09-05T00:00:00Z", null));
            window.Width = window.MinWidth;
            window.Height = window.MinHeight;
            window.UpdateLayout();
            SearchView search = Descendants(window).OfType<SearchView>().Single();
            TextBox query = Descendants(search).OfType<TextBox>().Single(control =>
                AutomationProperties.GetName(control) == UiStrings.SearchQuery);
            Assert.IsTrue(query.Focus());
            Assert.IsTrue(query.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)));
            Assert.IsInstanceOfType<System.Windows.Controls.Primitives.ToggleButton>(Keyboard.FocusedElement);
            Expander advanced = Descendants(search).OfType<Expander>().Single();
            Assert.IsFalse(advanced.IsExpanded);
            advanced.IsExpanded = true;
            window.UpdateLayout();
            query.Focus();
            query.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            ((UIElement)Keyboard.FocusedElement).MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            Assert.IsInstanceOfType<TextBox>(Keyboard.FocusedElement);
            Assert.AreEqual(UiStrings.SearchSection,
                AutomationProperties.GetName((DependencyObject)Keyboard.FocusedElement));
            viewModel.Search.Selected = viewModel.Search.Results[0];
            advanced.IsExpanded = false;
            query.Focus();
            window.UpdateLayout();
            Capture(window, "search");
            ListBox results = Descendants(search).OfType<ListBox>().Single();
            Assert.IsGreaterThanOrEqualTo(80.0, results.ActualHeight, "Search results must retain a usable viewport at minimum window size.");
            Assert.IsTrue(Descendants(search).OfType<TextBlock>().Any(text => text.Text == viewModel.Sync.Freshness.Status));
            Button syncLink = Descendants(search).OfType<Button>().Single(button =>
                AutomationProperties.GetName(button) == UiStrings.SearchGoToSync);
            Assert.IsNotNull(syncLink.Command);
            syncLink.Command.Execute(syncLink.CommandParameter);
            Assert.IsTrue(viewModel.IsLocalKnowledgeBaseVisible);
            Button settingsLink = Descendants(search).OfType<Button>().Single(button =>
                AutomationProperties.GetName(button) == UiStrings.SettingsNavigation);
            Assert.IsNotNull(settingsLink.Command);
            settingsLink.Command.Execute(settingsLink.CommandParameter);
            Assert.IsTrue(viewModel.IsSettingsVisible);
            viewModel.NavigateCommand.Execute(NavigationPage.History);
            Assert.IsTrue(viewModel.IsHistoryVisible);
            viewModel.History.Entries.Add(new OperationHistoryEntry("fixture", DateTimeOffset.UtcNow,
                UiStrings.HistoryLocal, UiStrings.HistoryPartial, UiStrings.FormatHistoryCounters(3, 0, 7, 0, 1),
                UiStrings.HistoryRetry, "operations/document.md", NavigationPage.LocalKnowledgeBase));
            window.UpdateLayout();
            Capture(window, "history");
        }
        finally
        {
            window?.Close();
            application.Shutdown();
        }
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, index);
            yield return child;
            foreach (DependencyObject descendant in Descendants(child)) { yield return descendant; }
        }
    }

    private static void Capture(Window window, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("CORTEX_VISUAL_ARTIFACTS");
        if (string.IsNullOrEmpty(directory)) { return; }
        Directory.CreateDirectory(directory);
        foreach (double scale in new[] { 1.0, 1.5, 2.0 })
        {
            RenderTargetBitmap bitmap = new((int)(window.ActualWidth * scale),
                (int)(window.ActualHeight * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
            bitmap.Render(window);
            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using FileStream stream = File.Create(Path.Combine(directory, $"{name}-{scale * 100:0}.png"));
            encoder.Save(stream);
        }
    }
}
