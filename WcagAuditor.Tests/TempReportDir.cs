namespace WcagAuditor.Tests;

/// <summary>
/// A scratch directory passed to the CLI via --report-dir so tests don't litter the repo
/// with saved report files, and are cleaned up regardless of test outcome.
/// </summary>
internal sealed class TempReportDir : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(), "wcag-auditor-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
