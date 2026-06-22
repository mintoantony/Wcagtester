using System.Text;

namespace WcagAuditor;

internal static class ReportFileWriter
{
    public static string Save(string reportText, Uri scannedUrl, DateTimeOffset scannedAt, string reportsDir)
    {
        Directory.CreateDirectory(reportsDir);
        var path = Path.Combine(reportsDir, BuildFileName(scannedUrl, scannedAt));
        File.WriteAllText(path, reportText);
        return path;
    }

    private static string BuildFileName(Uri scannedUrl, DateTimeOffset scannedAt)
    {
        var timestamp = scannedAt.ToString("yyyyMMdd_HHmmss");

        var host = SanitizeForFileName(scannedUrl.Host);
        var pathPart = SanitizeForFileName(scannedUrl.AbsolutePath.Trim('/'));
        var urlPart = host.Length == 0
            ? (pathPart.Length == 0 ? "url" : pathPart)
            : (pathPart.Length == 0 ? host : $"{host}_{pathPart}");

        if (urlPart.Length > 60)
        {
            urlPart = urlPart[..60];
        }

        return $"{timestamp}_{urlPart}.txt";
    }

    private static string SanitizeForFileName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            sb.Append(invalidChars.Contains(c) || c is '/' or '\\' or ' ' or ':' ? '-' : c);
        }
        return sb.ToString();
    }
}
