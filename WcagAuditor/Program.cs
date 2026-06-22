using Microsoft.Playwright;
using WcagAuditor;

CliOptions options;
try
{
    options = CliOptions.Parse(args);
}
catch (CliArgumentException ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 2;
}

try
{
    var scanner = new AccessibilityScanner();
    var result = await scanner.ScanAsync(options.Url, options.TimeoutMs, options.Headed);

    var scannedAt = DateTimeOffset.Now;
    var reportText = ReportFormatter.Build(result, options.Url, scannedAt);
    Console.WriteLine(reportText);

    var savedPath = ReportFileWriter.Save(reportText, options.Url, scannedAt, options.ReportDir);
    Console.WriteLine($"Report saved to: {savedPath}");

    return ReportFormatter.ComputeExitCode(result);
}
catch (TimeoutException)
{
    Console.Error.WriteLine(
        $"Error: navigating to '{options.Url}' timed out after {options.TimeoutMs}ms. " +
        "The page may be slow or unresponsive; try increasing --timeout-ms.");
    return 2;
}
catch (PlaywrightException ex) when (ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine(
        "Error: Playwright browser binaries are not installed. Run:\n" +
        "  pwsh bin/Debug/net10.0/playwright.ps1 install chromium\n" +
        "(see README.md for details)");
    return 2;
}
catch (PlaywrightException ex)
{
    Console.Error.WriteLine($"Error: could not reach '{options.Url}'. Check the URL and your network connection.\n(Details: {ex.Message})");
    return 2;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: unexpected failure: {ex.Message}");
    return 2;
}
