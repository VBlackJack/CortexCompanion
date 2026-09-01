// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Interfaces;

/// <summary>Abstracts native path selection so settings workflows remain testable.</summary>
public interface IFileDialogService
{
    /// <summary>Selects an existing Cortex executable.</summary>
    string? SelectCliExecutable(string? currentPath);

    /// <summary>Selects an existing knowledge-base directory.</summary>
    string? SelectKnowledgeBaseDirectory(string? currentPath);

    /// <summary>Selects the optional Confluence converter executable.</summary>
    string? SelectConfluenceConverterExecutable(string? currentPath);
}
