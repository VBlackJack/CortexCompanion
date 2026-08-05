// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows.Controls;

namespace CortexCompanion.Views;

/// <summary>
/// Displays the bounded scheduling projection without directly accessing Task Scheduler COM.
/// </summary>
public partial class SchedulingView : UserControl
{
    /// <summary>Initializes the scheduling view.</summary>
    public SchedulingView() => InitializeComponent();
}

