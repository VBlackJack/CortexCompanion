// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows;
using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Views;

namespace CortexCompanion.Services;

/// <summary>Presents the simple confirmation for deleting a recreatable scheduled task.</summary>
public sealed class SchedulingConfirmationService : ISchedulingConfirmationService
{
    /// <inheritdoc />
    public bool ConfirmDelete()
    {
        ConfirmationDialog dialog = ConfirmationDialog.CreateSimple(
            UiStrings.SchedulingDeleteConfirmationTitle,
            UiStrings.SchedulingDeleteConfirmation,
            true);
        dialog.Owner = Application.Current.MainWindow;
        return ConfirmationDialog.IsConfirmed(dialog.ShowDialog());
    }
}
