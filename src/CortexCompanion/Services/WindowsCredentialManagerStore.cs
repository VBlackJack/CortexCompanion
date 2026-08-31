// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Runtime.InteropServices;
using System.Security;
using CortexCompanion.Interfaces;

namespace CortexCompanion.Services;

/// <summary>
/// Stores the Confluence PAT in the current user's DPAPI-protected Windows Credential Manager.
/// </summary>
public sealed class WindowsCredentialManagerStore : IConfluenceCredentialStore
{
    internal const uint GenericCredentialType = 1;
    internal const uint LocalMachinePersistence = 2;
    internal const int MaximumCredentialBlobBytes = 2560;
    internal const string CredentialComment = "Cortex Confluence personal access token";
    internal const string CredentialUserName = "Cortex";

    private readonly NativeCredentialWriter _writeCredential;
    private readonly Func<int> _getLastError;

    /// <summary>Initializes the store with the native Windows credential API.</summary>
    public WindowsCredentialManagerStore()
        : this(CredWrite, Marshal.GetLastPInvokeError)
    {
    }

    internal WindowsCredentialManagerStore(
        NativeCredentialWriter writeCredential,
        Func<int> getLastError)
    {
        _writeCredential = writeCredential ?? throw new ArgumentNullException(nameof(writeCredential));
        _getLastError = getLastError ?? throw new ArgumentNullException(nameof(getLastError));
    }

    /// <inheritdoc />
    public async Task StoreAsync(
        string targetName,
        SecureString personalAccessToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(personalAccessToken);
        string normalizedTarget = targetName?.Trim()
            ?? throw new ArgumentNullException(nameof(targetName));
        if (normalizedTarget.Length == 0)
        {
            throw new ConfluenceCredentialStoreException(
                "The Windows credential target must not be empty.");
        }

        if (personalAccessToken.Length == 0)
        {
            throw new ConfluenceCredentialStoreException(
                "The Confluence personal access token must not be empty.");
        }

        int credentialBlobBytes = checked(personalAccessToken.Length * sizeof(char));
        if (credentialBlobBytes > MaximumCredentialBlobBytes)
        {
            throw new ConfluenceCredentialStoreException(
                "The Confluence personal access token exceeds the Windows credential limit.");
        }

        using SecureString tokenCopy = personalAccessToken.Copy();
        tokenCopy.MakeReadOnly();
        await Task.Run(
            () => StoreCore(normalizedTarget, tokenCopy, credentialBlobBytes),
            cancellationToken);
    }

    private void StoreCore(
        string targetName,
        SecureString personalAccessToken,
        int credentialBlobBytes)
    {
        IntPtr credentialBlob = IntPtr.Zero;
        try
        {
            credentialBlob = Marshal.SecureStringToGlobalAllocUnicode(personalAccessToken);
            NativeCredential credential = new()
            {
                Type = GenericCredentialType,
                TargetName = targetName,
                Comment = CredentialComment,
                CredentialBlobSize = checked((uint)credentialBlobBytes),
                CredentialBlob = credentialBlob,
                Persist = LocalMachinePersistence,
                UserName = CredentialUserName,
            };

            if (!_writeCredential(ref credential, flags: 0))
            {
                int errorCode = _getLastError();
                throw new ConfluenceCredentialStoreException(
                    $"Windows Credential Manager rejected the credential (system error {errorCode}).");
            }
        }
        finally
        {
            if (credentialBlob != IntPtr.Zero)
            {
                Marshal.ZeroFreeGlobalAllocUnicode(credentialBlob);
            }
        }
    }

    [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredWrite(ref NativeCredential credential, uint flags);
}

internal delegate bool NativeCredentialWriter(ref NativeCredential credential, uint flags);

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct NativeCredential
{
    internal uint Flags;
    internal uint Type;
    [MarshalAs(UnmanagedType.LPWStr)]
    internal string TargetName;
    [MarshalAs(UnmanagedType.LPWStr)]
    internal string? Comment;
    internal long LastWritten;
    internal uint CredentialBlobSize;
    internal IntPtr CredentialBlob;
    internal uint Persist;
    internal uint AttributeCount;
    internal IntPtr Attributes;
    [MarshalAs(UnmanagedType.LPWStr)]
    internal string? TargetAlias;
    [MarshalAs(UnmanagedType.LPWStr)]
    internal string UserName;
}
