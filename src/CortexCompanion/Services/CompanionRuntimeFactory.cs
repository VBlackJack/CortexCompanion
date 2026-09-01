// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Security.Principal;
using CortexCompanion.Constants;
using CortexCompanion.Interfaces;
using CortexCompanion.Logging;
using CortexCompanion.Models;
using CortexCompanion.ViewModels;

namespace CortexCompanion.Services;

/// <summary>Composes all CLI-bound features from one validated settings snapshot.</summary>
public sealed class CompanionRuntimeFactory : ICompanionRuntimeFactory
{
    private readonly AppPaths _paths;
    private readonly ICliHandshakeService _handshakeService;
    private readonly IProcessRunner _processRunner;
    private readonly IFileDialogService _fileDialogs;

    /// <summary>Initializes the factory with application-owned paths and process services.</summary>
    public CompanionRuntimeFactory(
        AppPaths paths,
        ICliHandshakeService handshakeService,
        IProcessRunner processRunner,
        IFileDialogService fileDialogs)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _handshakeService = handshakeService ?? throw new ArgumentNullException(nameof(handshakeService));
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _fileDialogs = fileDialogs ?? throw new ArgumentNullException(nameof(fileDialogs));
    }

    /// <inheritdoc />
    public CompanionRuntime CreatePending()
    {
        PagesViewModel pages = new(null, null, null, null, null, []);
        SyncViewModel sync = new(null, null, null, null, null, []);
        SchedulingViewModel scheduling = new(
            new TaskSchedulerComAdapter(),
            new SchedulingConfirmationService(),
            new ScheduledRunPersistence(_paths.ScheduledRunsDirectory),
            null,
            SchedulingEnvironmentInspector.GetActiveVariableNames());
        return new CompanionRuntime(
            pages,
            sync,
            scheduling,
            new CliHandshakeResult(CliHandshakeStatus.NotConfigured, null),
            null);
    }

    /// <inheritdoc />
    public async Task<CompanionRuntime> CreateAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        CliHandshakeResult handshake = await _handshakeService.EvaluateAsync(settings, cancellationToken);
        CliPathValidationResult cliValidation = CliPathValidator.Validate(settings.CliPath);
        ConfluenceConfigPathResolution? configPath = null;
        PagesViewModel pages;
        if (cliValidation.IsValid && cliValidation.AbsolutePath is not null)
        {
            configPath = ConfluenceConfigPathResolver.Resolve(cliValidation.AbsolutePath);
            IConfluenceCliClient cliClient = new ConfluenceCliClient(
                _processRunner,
                cliValidation.AbsolutePath,
                configPath.AbsolutePath);
            IConfluenceConfigStore configStore = new ConfluenceConfigStore(configPath.AbsolutePath);
            PagesMutationService mutations = new(
                cliClient,
                configStore,
                new PageMutationConfirmationService());
            ConfluenceSetupService setup = new(configStore);
            pages = new PagesViewModel(
                cliClient,
                mutations,
                setup,
                _fileDialogs,
                configPath,
                ConfluenceEnvironmentInspector.GetActiveOverrides());
        }
        else
        {
            pages = new PagesViewModel(
                null,
                null,
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
            syncCoordinator = new SyncRunCoordinator(_paths.SyncRunsDirectory, Environment.ProcessPath);
            interactiveLauncher = new InteractiveProcessLauncher();
        }

        SyncViewModel sync = new(
            syncCoordinator,
            interactiveLauncher,
            cliValidation.AbsolutePath,
            configPath?.AbsolutePath,
            ingestionPath,
            ConfluenceEnvironmentInspector.GetActiveOverrides());
        ScheduledTaskContract? scheduledTaskContract = BuildScheduledTaskContract(
            cliValidation,
            configPath,
            ingestionPath);
        SchedulingViewModel scheduling = new(
            new TaskSchedulerComAdapter(),
            new SchedulingConfirmationService(),
            new ScheduledRunPersistence(_paths.ScheduledRunsDirectory),
            scheduledTaskContract,
            SchedulingEnvironmentInspector.GetActiveVariableNames());

        await pages.InitializeAsync(handshake.IsReadOnly);
        await sync.InitializeAsync(handshake.IsReadOnly, cancellationToken);
        await scheduling.InitializeAsync(handshake.IsReadOnly, cancellationToken);
        return new CompanionRuntime(
            pages,
            sync,
            scheduling,
            handshake,
            cliValidation.AbsolutePath);
    }

    private ScheduledTaskContract? BuildScheduledTaskContract(
        CliPathValidationResult cliValidation,
        ConfluenceConfigPathResolution? configPath,
        IngestionPathResolution? ingestionPath)
    {
        if (!cliValidation.IsValid ||
            cliValidation.AbsolutePath is null ||
            configPath is null ||
            ingestionPath is null ||
            Environment.ProcessPath is null ||
            !File.Exists(configPath.AbsolutePath))
        {
            return null;
        }

        return new ScheduledTaskContract(
            Path.GetFullPath(Environment.ProcessPath),
            cliValidation.AbsolutePath,
            ingestionPath.ConfigPath,
            configPath.AbsolutePath,
            _paths.ScheduledRunsDirectory,
            AppConstants.IngestionSourceKind,
            WindowsIdentity.GetCurrent().Name);
    }
}
