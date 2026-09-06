// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Models;

/// <summary>Represents the explicit replacement selected in the source editor.</summary>
public sealed record SourceSelectionEdit(
    ConfluenceSelection Selection,
    IReadOnlyList<string> PageIds,
    string AdditionalReference,
    bool UpdateNow = false);
