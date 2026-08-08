// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using CortexCompanion.Commands;

namespace CortexCompanion.Tests.Commands;

[TestClass]
public sealed class AsyncRelayCommandTests
{
    [TestMethod]
    public async Task ExecuteAsyncContainsUnexpectedFailureAndRestoresCanExecute()
    {
        AsyncRelayCommand command = new(
            () => throw new InvalidOperationException("sensitive-message-must-not-escape"));
        int failureNotifications = 0;
        command.ExecutionFailed += (_, _) => failureNotifications++;

        await command.ExecuteAsync(parameter: null);

        Assert.AreEqual(1, failureNotifications);
        Assert.IsTrue(command.CanExecute(null));
    }

    [TestMethod]
    public async Task TypedExecuteAsyncContainsUnexpectedFailureAndRestoresCanExecute()
    {
        AsyncRelayCommand<string> command = new(
            _ => throw new InvalidOperationException("sensitive-message-must-not-escape"));
        int failureNotifications = 0;
        command.ExecutionFailed += (_, _) => failureNotifications++;

        await command.ExecuteAsync("value");

        Assert.AreEqual(1, failureNotifications);
        Assert.IsTrue(command.CanExecute("value"));
    }
}
