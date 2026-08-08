// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows.Input;
using CortexCompanion.Logging;

namespace CortexCompanion.Commands;

/// <summary>Runs one non-reentrant asynchronous UI action with a typed reference parameter.</summary>
public sealed class AsyncRelayCommand<T> : ICommand
    where T : class
{
    private readonly Func<T, Task> _execute;
    private readonly Func<T, bool>? _canExecute;
    private bool _isRunning;

    /// <summary>Initializes a typed asynchronous command.</summary>
    public AsyncRelayCommand(Func<T, Task> execute, Func<T, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <summary>Signals that an asynchronous UI action failed after the failure was contained.</summary>
    public event EventHandler? ExecutionFailed;

    /// <inheritdoc />
    public bool CanExecute(object? parameter) =>
        !_isRunning && parameter is T value && (_canExecute?.Invoke(value) ?? true);

    /// <inheritdoc />
    public async void Execute(object? parameter)
    {
        await ExecuteAsync(parameter);
    }

    /// <summary>Executes the command through an awaitable, exception-containing boundary.</summary>
    internal async Task ExecuteAsync(object? parameter)
    {
        if (parameter is not T value || !CanExecute(value))
        {
            return;
        }

        _isRunning = true;
        RaiseCanExecuteChanged();
        try
        {
            await _execute(value);
        }
        catch (Exception exception)
        {
            FileLogger.Error($"Asynchronous UI command failed ({exception.GetType().Name})");
            ExecutionFailed?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _isRunning = false;
            RaiseCanExecuteChanged();
        }
    }

    /// <summary>Re-evaluates command availability.</summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
