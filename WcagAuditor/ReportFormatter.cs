using System.Text;
using Deque.AxeCore.Commons;

namespace WcagAuditor;

internal static class ReportFormatter
{
    private const int HtmlSnippetMaxLength = 150;

    public static string Build(AxeResult result, Uri scannedUrl, DateTimeOffset scannedAt)
    {
        var sb = new StringBuilder();
        sb.AppendLine("WCAG 2.1 AA Accessibility Report");
        sb.AppendLine($"Scanned URL: {scannedUrl}");
        sb.AppendLine($"Scanned at:  {scannedAt:yyyy-MM-dd HH:mm:ss zzz}");

        var violations = result.Violations ?? Array.Empty<AxeResultItem>();

        if (violations.Length == 0)
        {
            sb.AppendLine();
            sb.AppendLine("No WCAG 2.1 AA violations detected by axe-core on this page.");
            sb.AppendLine("Note: automated tools catch roughly 30-50% of WCAG issues — manual review is still recommended.");
            return sb.ToString();
        }

        foreach (var violation in violations)
        {
            sb.AppendLine();
            sb.AppendLine($"[{violation.Impact?.ToUpperInvariant() ?? "UNKNOWN"}] {violation.Id} — {violation.Help}");
            sb.AppendLine($"  WCAG tags: {string.Join(", ", violation.Tags ?? Array.Empty<string>())}");
            sb.AppendLine($"  Help:      {violation.HelpUrl}");

            var nodes = violation.Nodes ?? Array.Empty<AxeResultNode>();
            sb.AppendLine($"  Affected elements ({nodes.Length}):");

            for (var i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                sb.AppendLine($"    {i + 1}. Selector: {node.Target?.Selector ?? "(unknown)"}");
                sb.AppendLine($"       HTML:     {Truncate(node.Html)}");
                sb.AppendLine($"       Fix:      {BuildFixSuggestion(node)}");
            }
        }

        sb.AppendLine();
        sb.AppendLine(BuildSummaryLine(violations));
        return sb.ToString();
    }

    public static int ComputeExitCode(AxeResult result) =>
        (result.Violations?.Length ?? 0) > 0 ? 1 : 0;

    private static string BuildFixSuggestion(AxeResultNode node)
    {
        var sb = new StringBuilder();
        AppendCheckGroup(sb, "Fix any of the following:", node.Any);
        AppendCheckGroup(sb, "Fix all of the following:", node.All);
        AppendCheckGroup(sb, "Fix the following:", node.None);

        return sb.Length == 0 ? "(no detailed guidance provided)" : sb.ToString().TrimEnd();
    }

    private static void AppendCheckGroup(StringBuilder sb, string preamble, AxeResultCheck[]? checks)
    {
        if (checks is null || checks.Length == 0)
        {
            return;
        }

        if (sb.Length > 0)
        {
            sb.Append("\n                ");
        }

        sb.Append(preamble);
        foreach (var check in checks)
        {
            sb.Append("\n                  ").Append(check.Message);
        }
    }

    private static string Truncate(string? html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return "(no markup captured)";
        }

        return html.Length <= HtmlSnippetMaxLength
            ? html
            : html[..HtmlSnippetMaxLength] + "…";
    }

    private static string BuildSummaryLine(AxeResultItem[] violations)
    {
        var byImpact = violations
            .GroupBy(v => v.Impact ?? "unknown")
            .Select(g => $"{g.Count()} {g.Key}")
            .ToArray();

        return $"{violations.Length} violation(s) found ({string.Join(", ", byImpact)})";
    }
}
