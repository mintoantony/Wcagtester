using Xunit;

namespace WcagAuditor.Tests;

/// <summary>
/// True end-to-end tests: each test spawns the actual built WcagAuditor.exe as a child
/// process (same as a user running it from a terminal) and asserts on its real stdout/stderr
/// and exit code. No internal types are referenced directly.
/// </summary>
public sealed class WcagAuditorE2ETests
{
    [Fact]
    public void Scan_BadFixture_ReportsAllFourSeededViolations_AndExitsWithCode1()
    {
        using var reportDir = new TempReportDir();
        var result = CliProcessRunner.Run(new[]
        {
            TestPaths.FixtureUrl("bad-accessibility.html"), "--report-dir", reportDir.Path,
        });

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("image-alt", result.StdOut);
        Assert.Contains("label", result.StdOut);
        Assert.Contains("color-contrast", result.StdOut);
        Assert.Contains("link-name", result.StdOut);
        Assert.Contains("4 violation(s) found", result.StdOut);
    }

    [Fact]
    public void Scan_CleanFixture_ReportsNoViolations_AndExitsWithCode0()
    {
        using var reportDir = new TempReportDir();
        var result = CliProcessRunner.Run(new[]
        {
            TestPaths.FixtureUrl("clean.html"), "--report-dir", reportDir.Path,
        });

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("No WCAG 2.1 AA violations detected", result.StdOut);
    }

    [Fact]
    public void Scan_CleanFixture_SavesReportFile_NamedWithTimestampAndUrl_ContainingFullReport()
    {
        using var reportDir = new TempReportDir();
        var result = CliProcessRunner.Run(new[]
        {
            TestPaths.FixtureUrl("clean.html"), "--report-dir", reportDir.Path,
        });

        Assert.Equal(0, result.ExitCode);

        var savedFiles = Directory.Exists(reportDir.Path) ? Directory.GetFiles(reportDir.Path, "*.txt") : Array.Empty<string>();
        var savedFile = Assert.Single(savedFiles);
        var fileName = Path.GetFileName(savedFile);
        Assert.Matches(@"^\d{8}_\d{6}_.+\.txt$", fileName);

        var savedContent = File.ReadAllText(savedFile);
        Assert.Contains("WCAG 2.1 AA Accessibility Report", savedContent);
        Assert.Contains("Scanned URL:", savedContent);
        Assert.Contains("Scanned at:", savedContent);
        Assert.Contains("No WCAG 2.1 AA violations detected", savedContent);

        Assert.Contains($"Report saved to: {savedFile}", result.StdOut);
    }

    [Fact]
    public void Run_WithNoArguments_PrintsUsageError_AndExitsWithCode2()
    {
        var result = CliProcessRunner.Run(Array.Empty<string>());

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("a URL argument is required", result.Combined);
    }

    [Fact]
    public void Run_WithNonNumericTimeout_PrintsValidationError_AndExitsWithCode2()
    {
        var result = CliProcessRunner.Run(new[] { TestPaths.FixtureUrl("clean.html"), "--timeout-ms", "not-a-number" });

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("--timeout-ms requires a positive integer", result.Combined);
    }

    [Fact]
    public void Run_WithExtraPositionalArgument_PrintsError_AndExitsWithCode2()
    {
        var result = CliProcessRunner.Run(new[] { TestPaths.FixtureUrl("clean.html"), "https://example.com" });

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("unexpected extra argument", result.Combined);
    }

    [Fact]
    public void Scan_UnreachableHost_ExitsWithCode2_AndExplainsFailure()
    {
        var result = CliProcessRunner.Run(
            new[] { "https://this-domain-does-not-exist-xyz123.invalid" },
            timeout: TimeSpan.FromSeconds(30));

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("could not reach", result.Combined);
    }

    [Fact]
    public void Scan_SlowLocalEndpoint_WithShortTimeout_ExitsWithCode2_AndMentionsTimeout()
    {
        using var server = new LocalTestServer { SlowDelayMs = 5000 };

        var result = CliProcessRunner.Run(
            new[] { server.SlowUrl, "--timeout-ms", "500" },
            timeout: TimeSpan.FromSeconds(30));

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("timed out", result.Combined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Scan_PageWithOutboundLink_NeverRequestsTheLinkedPage()
    {
        using var server = new LocalTestServer();
        using var reportDir = new TempReportDir();

        var result = CliProcessRunner.Run(
            new[] { server.BaseUrl, "--report-dir", reportDir.Path },
            timeout: TimeSpan.FromSeconds(30));

        Assert.Equal(0, result.ExitCode);
        Assert.True(server.RequestCount("/") >= 1, "expected the root page to have been requested");
        Assert.Equal(0, server.RequestCount("/other"));
    }
}
