namespace WcagAuditor;

internal sealed class CliArgumentException : Exception
{
    public CliArgumentException(string message) : base(message) { }
}

internal sealed class CliOptions
{
    private const string UsageMessage =
        "a URL argument is required.\nUsage: WcagAuditor <url> [--timeout-ms <n>] [--headed] [--report-dir <path>]";

    public required Uri Url { get; init; }
    public int TimeoutMs { get; init; } = 30000;
    public bool Headed { get; init; }
    public string ReportDir { get; init; } = "wcag-reports";

    public static CliOptions Parse(string[] args)
    {
        if (args.Length == 0)
        {
            throw new CliArgumentException(UsageMessage);
        }

        string? rawUrl = null;
        int timeoutMs = 30000;
        bool headed = false;
        string reportDir = "wcag-reports";

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--timeout-ms":
                    if (i + 1 >= args.Length || !int.TryParse(args[i + 1], out timeoutMs) || timeoutMs <= 0)
                    {
                        throw new CliArgumentException("--timeout-ms requires a positive integer value.");
                    }
                    i++;
                    break;
                case "--headed":
                    headed = true;
                    break;
                case "--report-dir":
                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]))
                    {
                        throw new CliArgumentException("--report-dir requires a folder path value.");
                    }
                    reportDir = args[i + 1];
                    i++;
                    break;
                default:
                    if (rawUrl is not null)
                    {
                        throw new CliArgumentException($"unexpected extra argument '{args[i]}'.");
                    }
                    rawUrl = args[i];
                    break;
            }
        }

        if (rawUrl is null)
        {
            throw new CliArgumentException(UsageMessage);
        }

        var url = ParseUrl(rawUrl);

        return new CliOptions { Url = url, TimeoutMs = timeoutMs, Headed = headed, ReportDir = reportDir };
    }

    private static Uri ParseUrl(string rawUrl)
    {
        if (Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri) && IsSupportedScheme(uri))
        {
            return uri;
        }

        var withScheme = "https://" + rawUrl;
        if (Uri.TryCreate(withScheme, UriKind.Absolute, out var fallbackUri) && IsSupportedScheme(fallbackUri))
        {
            return fallbackUri;
        }

        if (uri is not null && !IsSupportedScheme(uri))
        {
            throw new CliArgumentException("only http, https, and file URLs are supported.");
        }

        throw new CliArgumentException($"'{rawUrl}' is not a valid absolute URL. Did you mean 'https://{rawUrl}'?");
    }

    // file:// is allowed alongside http/https so a local HTML fixture can be scanned without a server.
    private static bool IsSupportedScheme(Uri uri) =>
        uri.Scheme is "http" or "https" or "file";
}
