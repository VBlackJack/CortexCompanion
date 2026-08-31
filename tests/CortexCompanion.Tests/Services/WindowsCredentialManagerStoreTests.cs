// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Runtime.InteropServices;
using System.Security;
using CortexCompanion.Services;

namespace CortexCompanion.Tests.Services;

/// <summary>Guards the native credential shape and secret-redaction boundary.</summary>
[TestClass]
public sealed class WindowsCredentialManagerStoreTests
{
    [TestMethod]
    public async Task StoreAsyncWritesCortexCompatibleGenericCredential()
    {
        const string expectedToken = "unit-test-pat";
        string? capturedToken = null;
        NativeCredential capturedCredential = default;
        uint capturedFlags = uint.MaxValue;
        bool WriteCredential(ref NativeCredential credential, uint flags)
        {
            capturedCredential = credential;
            capturedFlags = flags;
            capturedToken = Marshal.PtrToStringUni(
                credential.CredentialBlob,
                checked((int)credential.CredentialBlobSize / sizeof(char)));
            return true;
        }

        WindowsCredentialManagerStore store = new(WriteCredential, () => 0);
        using SecureString secureToken = CreateSecureString(expectedToken);

        await store.StoreAsync("cortex-confluence", secureToken);

        Assert.AreEqual(0U, capturedFlags);
        Assert.AreEqual(WindowsCredentialManagerStore.GenericCredentialType, capturedCredential.Type);
        Assert.AreEqual(
            WindowsCredentialManagerStore.LocalMachinePersistence,
            capturedCredential.Persist);
        Assert.AreEqual("cortex-confluence", capturedCredential.TargetName);
        Assert.AreEqual(WindowsCredentialManagerStore.CredentialComment, capturedCredential.Comment);
        Assert.AreEqual(WindowsCredentialManagerStore.CredentialUserName, capturedCredential.UserName);
        Assert.AreEqual(
            checked((uint)(expectedToken.Length * sizeof(char))),
            capturedCredential.CredentialBlobSize);
        Assert.AreEqual(expectedToken, capturedToken);
    }

    [TestMethod]
    public async Task StoreAsyncFailureDoesNotExposePatInException()
    {
        const string personalAccessToken = "secret-must-remain-redacted";
        bool WriteCredential(ref NativeCredential credential, uint flags) => false;
        WindowsCredentialManagerStore store = new(WriteCredential, () => 5);
        using SecureString secureToken = CreateSecureString(personalAccessToken);

        ConfluenceCredentialStoreException exception =
            await Assert.ThrowsAsync<ConfluenceCredentialStoreException>(() =>
                store.StoreAsync("cortex-confluence", secureToken));

        Assert.DoesNotContain(personalAccessToken, exception.ToString(), StringComparison.Ordinal);
        Assert.Contains("system error 5", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task StoreAsyncRejectsOversizedPatBeforeNativeWrite()
    {
        bool writerCalled = false;
        bool WriteCredential(ref NativeCredential credential, uint flags)
        {
            writerCalled = true;
            return true;
        }

        WindowsCredentialManagerStore store = new(WriteCredential, () => 0);
        using SecureString secureToken = CreateSecureString(new string('x', 1281));

        await Assert.ThrowsAsync<ConfluenceCredentialStoreException>(() =>
            store.StoreAsync("cortex-confluence", secureToken));

        Assert.IsFalse(writerCalled);
    }

    private static SecureString CreateSecureString(string value)
    {
        SecureString secret = new();
        foreach (char character in value)
        {
            secret.AppendChar(character);
        }

        secret.MakeReadOnly();
        return secret;
    }
}
