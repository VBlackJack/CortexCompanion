// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows;
using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using CortexCompanion.Views;

namespace CortexCompanion.Services;

/// <summary>Presents the confirmation for a change of what a section indexes.</summary>
public sealed class SectionRootConfirmationService : ISectionRootConfirmationService
{
    /// <inheritdoc />
    public bool ConfirmMount(string section, string root) =>
        Confirm(UiStrings.FormatSettingsSectionConfirmMount(section, root));

    /// <inheritdoc />
    public bool ConfirmUseDefault(string section, string defaultRoot) =>
        Confirm(UiStrings.FormatSettingsSectionConfirmDefault(section, defaultRoot));

    private static bool Confirm(string message)
    {
        ConfirmationDialog dialog = ConfirmationDialog.CreateSimple(
            UiStrings.SettingsSectionConfirmTitle,
            message,
            false);
        dialog.Owner = Application.Current.MainWindow;
        return ConfirmationDialog.IsConfirmed(dialog.ShowDialog());
    }
}
