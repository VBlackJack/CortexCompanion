// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows;
using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Models;
using CortexCompanion.Views;

namespace CortexCompanion.Services;

/// <summary>Presents the localized consequences of stopping or abandoning a run.</summary>
public sealed class RunInterruptionConfirmationService : IRunInterruptionConfirmationService
{
    /// <inheritdoc />
    public bool ConfirmStop(SyncRunKind runKind)
    {
        ConfirmationDialog dialog = ConfirmationDialog.CreateSimple(
            UiStrings.SyncCancelConfirmTitle,
            runKind == SyncRunKind.LocalDocuments
                ? UiStrings.SyncCancelConfirmLocal
                : UiStrings.SyncCancelConfirmConfluence,
            true);
        dialog.Owner = Application.Current.MainWindow;
        return ConfirmationDialog.IsConfirmed(dialog.ShowDialog());
    }

    /// <inheritdoc />
    public bool ConfirmCloseWhileRunning()
    {
        ConfirmationDialog dialog = ConfirmationDialog.CreateSimple(
            UiStrings.CloseDuringRunTitle,
            UiStrings.CloseDuringRunMessage,
            false);
        dialog.Owner = Application.Current.MainWindow;
        return ConfirmationDialog.IsConfirmed(dialog.ShowDialog());
    }
}
