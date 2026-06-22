namespace WcagAuditor.Tests;

internal static class TestPaths
{
    private static readonly string RepoRoot = FindRepoRoot();

    public static string FixturesDir { get; } = Path.Combine(RepoRoot, "test-fixtures");

    public static string ExePath { get; } = ResolveExePath();

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "test-fixtures")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not locate the 'test-fixtures' directory above the test assembly.");
    }

    private static string ResolveExePath()
    {
        // The ProjectReference to WcagAuditor.csproj copies its build output (including the
        // executable) next to the test assembly, so dotnet test always exercises a fresh build.
        var candidate = Path.Combine(AppContext.BaseDirectory, "WcagAuditor.exe");
        if (!File.Exists(candidate))
        {
            throw new FileNotFoundException(
                "Expected WcagAuditor.exe next to the test assembly (via ProjectReference output copy). " +
                "Did the WcagAuditor project fail to build?", candidate);
        }
        return candidate;
    }

    public static string FixtureUrl(string fileName) =>
        new Uri(Path.Combine(FixturesDir, fileName)).AbsoluteUri;
}
