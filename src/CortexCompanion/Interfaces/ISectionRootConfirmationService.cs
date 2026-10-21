// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Interfaces;

/// <summary>Abstracts the explicit confirmation required before a section changes the folder it is read from.</summary>
public interface ISectionRootConfirmationService
{
    /// <summary>Confirms mounting one section on a folder outside the document folder.</summary>
    bool ConfirmMount(string section, string root);

    /// <summary>Confirms returning one section to its folder in the document folder.</summary>
    bool ConfirmUseDefault(string section, string defaultRoot);
}
