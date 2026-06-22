using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace WcagAuditor;

internal sealed class AccessibilityScanner
{
    private static readonly string[] Wcag21AaTags = { "wcag2a", "wcag2aa", "wcag21a", "wcag21aa" };

    // Single GotoAsync call below — by design this scanner never follows links or queues further navigation.
    public async Task<AxeResult> ScanAsync(Uri url, int timeoutMs, bool headed)
    {
        using var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = !headed,
        });

        try
        {
            var page = await browser.NewPageAsync();
            await page.GotoAsync(url.ToString(), new PageGotoOptions
            {
                Timeout = timeoutMs,
                WaitUntil = WaitUntilState.NetworkIdle,
            });

            var options = new AxeRunOptions
            {
                RunOnly = RunOnlyOptions.Tags(Wcag21AaTags),
            };

            return await page.RunAxe(options);
        }
        finally
        {
            await browser.CloseAsync();
        }
    }
}
