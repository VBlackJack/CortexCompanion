// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Text;
using CortexCompanion.Models;
using CortexCompanion.Services;

namespace CortexCompanion.Tests.Services;

[TestClass]
public sealed class ConfluenceConfigParserRendererTests
{
    [TestMethod]
    public void GoldenCasesMatchPythonRendererBytes()
    {
        ConfluenceConfiguration configuration = new(
            2,
            "https://wiki.example.test:8443/confluence",
            "l'equipe-東京",
            new DateTimeOffset(2026, 8, 5, 12, 13, 14, 123, TimeSpan.FromHours(2.5)).AddTicks(4560),
            @"C:\Program Files\Cortex\console.exe",
            1,
            0.0000001,
            [
                new ConfluenceSpaceConfiguration(
                    "DOC.UNICODE",
                    "équipe/docs",
                    "pro-confidentiel",
                    ConfluenceSelection.Pages,
                    ["123", "987654321"]),
                new ConfluenceSpaceConfiguration(
                    "EMPTY",
                    "empty",
                    "perso-non-sensible",
                    ConfluenceSelection.Pages,
                    []),
                new ConfluenceSpaceConfiguration(
                    "ALL",
                    "all",
                    "pro-confidentiel",
                    ConfluenceSelection.WholeSpace,
                    []),
            ]);
        string expected = """
            schema_version = 2
            base_url = "https://wiki.example.test:8443/confluence"
            credential_target = "l'equipe-東京"
            auth_expires_at = "2026-08-05T12:13:14.123456+02:30"
            console_path = "C:\\Program Files\\Cortex\\console.exe"
            max_attachment_size_mb = 1
            failure_threshold = 1e-07

            [[spaces]]
            space_key = "DOC.UNICODE"
            target = "équipe/docs"
            classification = "pro-confidentiel"
            selection = "pages"

            [[spaces.pages]]
            page_id = "123"

            [[spaces.pages]]
            page_id = "987654321"

            [[spaces]]
            space_key = "EMPTY"
            target = "empty"
            classification = "perso-non-sensible"
            selection = "pages"
            pages = []

            [[spaces]]
            space_key = "ALL"
            target = "all"
            classification = "pro-confidentiel"
            selection = "whole_space"
            """.Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";

        byte[] rendered = ConfluenceConfigRenderer.Render(configuration);

        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes(expected), rendered);
        Assert.IsTrue(ConfluenceConfigParser.Parse(rendered, "golden.toml").SemanticallyEquals(configuration));
    }

    [TestMethod]
    public void VersionOneDefaultsAndLegacySpaceRoundTripWithoutV2Fields()
    {
        byte[] source = Encoding.UTF8.GetBytes("""
            schema_version = 1
            base_url = "https://wiki.example.test/"

            [[spaces]]
            space_key = "DOC"
            target = "docs"
            classification = "pro-confidentiel"
            """ + "\n");

        ConfluenceConfiguration parsed = ConfluenceConfigParser.Parse(source, "v1.toml");
        string rendered = Encoding.UTF8.GetString(ConfluenceConfigRenderer.Render(parsed));

        Assert.AreEqual(1, parsed.SchemaVersion);
        Assert.AreEqual("https://wiki.example.test", parsed.BaseUrl);
        Assert.AreEqual("cortex-spike", parsed.CredentialTarget);
        Assert.AreEqual(50, parsed.MaxAttachmentSizeMb);
        Assert.AreEqual(0.1, parsed.FailureThreshold);
        Assert.IsFalse(rendered.Contains("selection", StringComparison.Ordinal));
        Assert.IsFalse(rendered.Contains("pages", StringComparison.Ordinal));
    }

    [TestMethod]
    public void VersionTwoWholeSpaceWithPagesIsRejected()
    {
        byte[] source = Encoding.UTF8.GetBytes("""
            schema_version = 2
            [[spaces]]
            space_key = "DOC"
            target = "docs"
            classification = "pro-confidentiel"
            selection = "whole_space"
            pages = []
            """);

        Assert.Throws<ConfluenceConfigValidationException>(() =>
            ConfluenceConfigParser.Parse(source, "invalid.toml"));
    }

    [TestMethod]
    public void BareTomlOffsetDateTimeIsAcceptedButLocalDateTimeIsRejected()
    {
        byte[] withOffset = Encoding.UTF8.GetBytes("auth_expires_at = 2026-08-05T12:13:14+02:00\n");
        byte[] withoutOffset = Encoding.UTF8.GetBytes("auth_expires_at = 2026-08-05T12:13:14\n");

        ConfluenceConfiguration parsed = ConfluenceConfigParser.Parse(withOffset, "offset.toml");

        Assert.AreEqual(TimeSpan.FromHours(2), parsed.AuthExpiresAt!.Value.Offset);
        Assert.Throws<ConfluenceConfigValidationException>(() =>
            ConfluenceConfigParser.Parse(withoutOffset, "local.toml"));
    }
}
