// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Localization;
using CortexCompanion.Models;

namespace CortexCompanion.ViewModels;

/// <summary>Presents one indexed section and the folder Cortex reads it from.</summary>
public sealed record SectionRootItem(SectionRoot Entry, bool IndexWholeFolder)
{
    /// <summary>Gets the logical section name.</summary>
    public string Section => Entry.Section;

    /// <summary>Gets the resolved folder, or why there is none yet.</summary>
    public string RootText => Entry.Root ?? UiStrings.SettingsSectionRootUnknown;

    /// <summary>Gets whether the section is mounted outside the document folder.</summary>
    public bool IsMounted => Entry.Mounted;

    /// <summary>Gets whether Cortex resolved a folder that does not exist, so the section is not indexed.</summary>
    public bool IsMissing => Entry.Root is not null && !Entry.Exists;
}
