# WcagAuditor

A command-line tool that loads a single web page and audits it for WCAG 2.1 AA accessibility violations using [axe-core](https://github.com/dequelabs/axe-core), reporting each violation with the affected element(s) and a suggested fix.

**This tool only audits the URL you give it.** It does not crawl or test any pages linked from that page.

## Requirements

- .NET 10 SDK
- One-time Playwright browser install (see below)

## Setup

```powershell
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install chromium
```

The second command downloads the Chromium browser binary Playwright uses to render pages. It only needs to be run once per machine/dev environment, not on every build.

## Usage

```powershell
dotnet run -- <url> [--timeout-ms <n>] [--headed]
```

- `<url>` — the page to audit. `http://`, `https://`, and `file://` URLs are supported. If you omit the scheme (e.g. `example.com`), `https://` is assumed.
- `--timeout-ms <n>` — navigation timeout in milliseconds (default `30000`).
- `--headed` — run the browser visibly instead of headless, useful for debugging.

Example:

```powershell
dotnet run -- https://example.com
```

## Output

Each violation is printed with its rule ID, WCAG-tag-derived impact level, a help link, every affected element (CSS selector + truncated HTML), and a "Fix" suggestion assembled from axe-core's check messages. A summary line totals violations by impact.

## Exit codes

| Code | Meaning |
|---|---|
| `0` | Scan completed, no WCAG 2.1 AA violations found |
| `1` | Scan completed, one or more violations found |
| `2` | Boundary error — bad/missing URL, unreachable host, navigation timeout, or Playwright browser not installed |

## Caveat

Automated tools like axe-core catch roughly 30-50% of WCAG issues. A clean report is not a guarantee of full accessibility — manual review is still recommended.
