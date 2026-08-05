// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Interfaces;
using CortexCompanion.Models;

namespace CortexCompanion.Tests.TestSupport;

internal sealed class StubProcessRunner(ProcessRunResult result) : IProcessRunner
{
    public int CallCount { get; private set; }

    public ProcessRequest? LastRequest { get; private set; }

    public Task<ProcessRunResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CallCount++;
        LastRequest = request;
        return Task.FromResult(result);
    }
}

