// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows;
using CortexCompanion.Services;

namespace CortexCompanion.Views;

/// <summary>Presents simple and typed confirmations through one themed window.</summary>
public partial class ConfirmationDialog : Window
{
    private ConfirmationDialog(
        string title,
        string message,
        bool usesTypedInput,
        bool showsDestructiveEmphasis,
        string? inputLabel)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        TypedInputPanel.Visibility = usesTypedInput ? Visibility.Visible : Visibility.Collapsed;
        DestructiveIndicator.Visibility = showsDestructiveEmphasis ? Visibility.Visible : Visibility.Collapsed;
        InputLabelText.Text = inputLabel ?? string.Empty;
        SourceInitialized += (_, _) => DarkTitleBarService.Apply(this);
    }

    /// <summary>Creates a two-button confirmation without typed input.</summary>
    public static ConfirmationDialog CreateSimple(string title, string message, bool isDestructive) =>
        new(title, message, false, isDestructive, null);

    /// <summary>Creates a confirmation that returns the exact text entered by the user.</summary>
    public static ConfirmationDialog CreateTyped(string title, string message, string inputLabel) =>
        new(title, message, true, false, inputLabel);

    /// <summary>Gets the raw value typed by the user.</summary>
    public string ConfirmationText => ConfirmationInput.Text;

    /// <summary>Maps a modal result so that only explicit confirmation can authorize a mutation.</summary>
    public static bool IsConfirmed(bool? dialogResult) => dialogResult == true;

    private void ConfirmClick(object sender, RoutedEventArgs e) => DialogResult = true;
}
