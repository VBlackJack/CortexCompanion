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
            viewModel.NavigateCommand.Execute(NavigationPage.ConfluencePages);
            viewModel.Pages.SourceUrl = "https://wiki.example.test/spaces/DOC/overview";
            window.UpdateLayout();
            Capture(window, "confluence");
            viewModel.Pages.Spaces.Add(new ConfiguredSpaceViewModel("DOC", "confluence/DOC", "pro-confidentiel",
                ConfluenceSelection.Subtree, [new ConfiguredPageViewModel("DOC", "100", "Installation et administration des serveurs Windows", null)]));
            window.UpdateLayout();
            PagesView pagesView = Descendants(window).OfType<PagesView>().Single();
            ScrollViewer sourceScroll = Descendants(pagesView).OfType<ScrollViewer>().First();
            sourceScroll.ScrollToTop();
            window.UpdateLayout();
            Assert.IsTrue(Descendants(pagesView).OfType<Button>().Any(button => Equals(button.Content, UiStrings.ManageEdit)));
            Capture(window, "my-sources");
            SourceSelectionDialog editor = new(new ConfluenceSpaceConfiguration("DOC", "confluence/DOC", "pro-confidentiel",
                ConfluenceSelection.Subtree, ["100"]), [new ConfiguredPageContract { PageId = "100", Title = "Installation et administration des serveurs Windows" }], () => Task.FromResult(
                    new ConfluenceCliResult<SourceCatalogContract>(CortexExitCode.Ok, new SourceCatalogContract
                    {
                        ContractVersion = 1,
                        SpaceKey = "DOC",
                        Pages = [
                            new CatalogPageContract { PageId = "100", Title = "Installation et administration des serveurs Windows", AncestorIds = [] },
                            new CatalogPageContract { PageId = "101", Title = "Configurer WinRM", AncestorIds = ["100"] },
                            new CatalogPageContract { PageId = "102", Title = "Tester la connexion", AncestorIds = ["100", "101"] }],
                    }, string.Empty, false, null)))
            { Owner = window, ShowInTaskbar = false };
            try
            {
                editor.Show();
                editor.UpdateLayout();
                Assert.IsTrue(((RadioButton)editor.FindName("SubtreeOption")).IsChecked);
                Assert.IsTrue(Descendants(editor).OfType<CheckBox>().Single().IsChecked);
                ((Button)editor.FindName("LoadTreeButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                editor.UpdateLayout();
                Assert.HasCount(3, Descendants(editor).OfType<CheckBox>());
                Capture(editor, "source-editor");
                ((TextBox)editor.FindName("TreeSearch")).Text = "Tester";
                editor.UpdateLayout();
                Capture(editor, "source-tree-filtered");
            }
            finally { editor.Close(); }
            SourceChangeReview review = SourceChangeReview.Create(
                new ConfluenceSpaceConfiguration("DOC", "doc", "pro-confidentiel", ConfluenceSelection.WholeSpace, []),
                new ConfluenceSpaceConfiguration("DOC", "doc", "pro-confidentiel", ConfluenceSelection.Pages, ["100"]),
                new SourceCatalogContract
                {
                    ContractVersion = 1,
                    SpaceKey = "DOC",
                    Pages = [
                    new CatalogPageContract { PageId = "100", Title = "Documentation Windows", AncestorIds = [] },
                    new CatalogPageContract { PageId = "101", Title = "Configurer WinRM", AncestorIds = ["100"] }]
                }, []);
            SourceChangeReviewDialog reviewDialog = new(review) { Owner = window, ShowInTaskbar = false };
            try { reviewDialog.Show(); reviewDialog.UpdateLayout(); Capture(reviewDialog, "source-review"); }
            finally { reviewDialog.Close(); }
            ScopeSelectionDialog scope = new(new ScopePreviewContract
            {
                ContractVersion = 1,
                PageId = "100",
                Title = "Documentation de l'équipe",
                SpaceKey = "DOC",
                RecommendedSelection = "subtree",
                StorageRoot = temporary.Path,
                RetentionGenerations = 2,
                PageOnly = new() { PageCount = 1, EstimatedBytes = 393216 },
                Subtree = new() { PageCount = 12, EstimatedBytes = 4718592 },
                WholeSpace = new() { PageCount = 200, EstimatedBytes = 78643200 },
            })
            { Owner = window, ShowInTaskbar = false };
            try
            {
                scope.Show();
                scope.UpdateLayout();
                Button confirm = (Button)scope.FindName("AddSelectionButton");
                Assert.AreEqual(UiStrings.SourcesSaveUpdate, confirm.Content);
                Assert.AreEqual(UiStrings.FormatFlowConfirmCount(12), confirm.ToolTip);
                ((RadioButton)scope.FindName("WholeSpaceOption")).IsChecked = true;
                Assert.AreEqual(UiStrings.FormatFlowConfirmCount(200), confirm.ToolTip);
                scope.UpdateLayout();
                Capture(scope, "confluence-scope");
            }
            finally { scope.Close(); }
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
