using System.Diagnostics;

namespace WcagAuditor.Tests;

internal sealed record CliResult(int ExitCode, string StdOut, string StdErr)
{
    public string Combined => StdOut + StdErr;
}

internal static class CliProcessRunner
{
    public static CliResult Run(IEnumerable<string> args, TimeSpan? timeout = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = TestPaths.ExePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start process '{TestPaths.ExePath}'.");

        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();

        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(45);
        if (!process.WaitForExit((int)effectiveTimeout.TotalMilliseconds))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(
                $"WcagAuditor did not exit within {effectiveTimeout}. Args: {string.Join(' ', args)}");
        }

        // WaitForExit(int) can return before async stream reads finish flushing; this call
        // (no timeout) blocks until the redirected stdout/stderr pipes are fully drained.
        process.WaitForExit();

        return new CliResult(process.ExitCode, stdOutTask.GetAwaiter().GetResult(), stdErrTask.GetAwaiter().GetResult());
    }
}
