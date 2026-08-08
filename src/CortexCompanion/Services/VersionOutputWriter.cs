// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Runtime.InteropServices;
using System.Text;

namespace CortexCompanion.Services;

/// <summary>Writes the installer validation token from a Windows-subsystem executable.</summary>
public static class VersionOutputWriter
{
    private const int StandardOutputHandle = -11;
    private static readonly nint InvalidHandleValue = new(-1);

    /// <summary>Writes one UTF-8 line to an inherited stdout pipe without opening a console window.</summary>
    public static bool TryWriteLine(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        nint handle = GetStdHandle(StandardOutputHandle);
        if (handle == nint.Zero || handle == InvalidHandleValue)
        {
            return false;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(string.Concat(value, "\n"));
        return WriteFile(
            handle,
            bytes,
            checked((uint)bytes.Length),
            out uint written,
            nint.Zero) &&
            written == bytes.Length;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GetStdHandle(int standardHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WriteFile(
        nint file,
        byte[] buffer,
        uint numberOfBytesToWrite,
        out uint numberOfBytesWritten,
        nint overlapped);
}
