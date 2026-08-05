// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Localization;

namespace CortexCompanion.ViewModels;

/// <summary>Projects one supported environment override as a locked root field.</summary>
public sealed record EnvironmentOverrideViewModel(string FieldName, string EnvironmentName, string Value)
{
    /// <summary>Gets the localized locked-origin label.</summary>
    public string DisplayOrigin => UiStrings.FormatEnvironmentOverrideOrigin(EnvironmentName);
}
