// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Constants;
using CortexCompanion.Interfaces;
using CortexCompanion.Localization;
using Microsoft.Win32;

namespace CortexCompanion.Services;

/// <summary>Presents native Windows pickers for settings-owned paths.</summary>
public sealed class FileDialogService : IFileDialogService
{
    /// <inheritdoc />
    public string? SelectCliExecutable(string? currentPath)
    {
        OpenFileDialog dialog = new()
        {
            CheckFileExists = true,
            FileName = Path.GetFileName(currentPath) ?? AppConstants.CliExecutableName,
            Filter = UiStrings.CliFileDialogFilter,
            Multiselect = false,
            Title = UiStrings.CliFileDialogTitle,
        };
        SetInitialDirectory(dialog, currentPath);
        return NormalizeSelection(dialog.ShowDialog(), dialog.FileName);
    }

    /// <inheritdoc />
    public string? SelectKnowledgeBaseDirectory(string? currentPath)
    {
        OpenFolderDialog dialog = new()
        {
            InitialDirectory = Directory.Exists(currentPath) ? currentPath : null,
            Multiselect = false,
            Title = UiStrings.KnowledgeBaseFolderDialogTitle,
        };
        return dialog.ShowDialog() == true ? Path.GetFullPath(dialog.FolderName) : null;
    }

    /// <inheritdoc />
    public string? SelectConfluenceConverterExecutable(string? currentPath)
    {
        OpenFileDialog dialog = new()
        {
            CheckFileExists = true,
            FileName = Path.GetFileName(currentPath) ?? AppConstants.ConfluenceConverterExecutableName,
            Filter = UiStrings.ConfluenceConverterFileDialogFilter,
            Multiselect = false,
            Title = UiStrings.ConfluenceConverterFileDialogTitle,
        };
        SetInitialDirectory(dialog, currentPath);
        return NormalizeSelection(dialog.ShowDialog(), dialog.FileName);
    }

    private static void SetInitialDirectory(FileDialog dialog, string? currentPath)
    {
        string? directory = string.IsNullOrWhiteSpace(currentPath)
            ? null
            : Path.GetDirectoryName(currentPath);
        if (Directory.Exists(directory))
        {
            dialog.InitialDirectory = directory;
        }
    }

    private static string? NormalizeSelection(bool? accepted, string? path) =>
        accepted == true && !string.IsNullOrWhiteSpace(path) ? Path.GetFullPath(path) : null;
}
