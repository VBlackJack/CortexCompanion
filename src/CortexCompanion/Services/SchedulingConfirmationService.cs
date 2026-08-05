// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows;
using CortexCompanion.Interfaces;
using CortexCompanion.Localization;

namespace CortexCompanion.Services;

/// <summary>Presents the simple confirmation for deleting a recreatable scheduled task.</summary>
public sealed class SchedulingConfirmationService : ISchedulingConfirmationService
{
    /// <inheritdoc />
    public bool ConfirmDelete() => MessageBox.Show(
        Application.Current.MainWindow,
        UiStrings.SchedulingDeleteConfirmation,
        UiStrings.SchedulingDeleteConfirmationTitle,
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning,
        MessageBoxResult.No) == MessageBoxResult.Yes;
}
