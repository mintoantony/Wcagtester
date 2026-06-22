using System.Text;
using Deque.AxeCore.Commons;

namespace WcagAuditor;

internal sealed class ConsoleReporter
{
    private const int HtmlSnippetMaxLength = 150;

    public void Report(AxeResult result, Uri scannedUrl)
    {
        var violations = result.Violations ?? Array.Empty<AxeResultItem>();

        if (violations.Length == 0)
        {
            Console.WriteLine("No WCAG 2.1 AA violations detected by axe-core on this page.");
            Console.WriteLine("Note: automated tools catch roughly 30-50% of WCAG issues — manual review is still recommended.");
            Console.WriteLine($"Scanned: {scannedUrl}");
            return;
        }

        foreach (var violation in violations)
        {
            Console.WriteLine();
            Console.WriteLine($"[{violation.Impact?.ToUpperInvariant() ?? "UNKNOWN"}] {violation.Id} — {violation.Help}");
            Console.WriteLine($"  WCAG tags: {string.Join(", ", violation.Tags ?? Array.Empty<string>())}");
            Console.WriteLine($"  Help:      {violation.HelpUrl}");

            var nodes = violation.Nodes ?? Array.Empty<AxeResultNode>();
            Console.WriteLine($"  Affected elements ({nodes.Length}):");

            for (var i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                Console.WriteLine($"    {i + 1}. Selector: {node.Target?.Selector ?? "(unknown)"}");
                Console.WriteLine($"       HTML:     {Truncate(node.Html)}");
                Console.WriteLine($"       Fix:      {BuildFixSuggestion(node)}");
            }
        }

        Console.WriteLine();
        Console.WriteLine(BuildSummaryLine(violations));
        Console.WriteLine($"Scanned: {scannedUrl}");
    }

    public int ComputeExitCode(AxeResult result) =>
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
