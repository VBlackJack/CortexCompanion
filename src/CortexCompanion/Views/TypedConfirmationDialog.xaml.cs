// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows;
using CortexCompanion.Services;

namespace CortexCompanion.Views;

/// <summary>Collects an exact typed space-key confirmation.</summary>
public partial class TypedConfirmationDialog : Window
{
    /// <summary>Initializes the dialog with the exact mode consequence.</summary>
    public TypedConfirmationDialog(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
        SourceInitialized += (_, _) => DarkTitleBarService.Apply(this);
    }

    /// <summary>Gets the raw value typed by the user.</summary>
    public string ConfirmationText => ConfirmationInput.Text;

    private void ConfirmClick(object sender, RoutedEventArgs e) => DialogResult = true;

    private void CancelClick(object sender, RoutedEventArgs e) => DialogResult = false;
}
