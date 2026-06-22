using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WcagAuditor.Tests;

/// <summary>
/// Minimal raw-socket HTTP server (no HttpListener/http.sys, so it needs no URL-ACL/admin
/// rights) used to give the CLI a fully local, deterministic page to scan: one with an
/// outbound link (to prove the scanner never follows it) and a slow endpoint (to trigger a
/// real navigation timeout without depending on a flaky third-party host).
/// </summary>
internal sealed class LocalTestServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _acceptLoop;
    private readonly ConcurrentDictionary<string, int> _requestCounts = new();

    public int SlowDelayMs { get; set; } = 5000;
    public int Port { get; }
    public string BaseUrl => $"http://127.0.0.1:{Port}/";
    public string SlowUrl => $"{BaseUrl}slow";

    public LocalTestServer()
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_cts.Token));
    }

    public int RequestCount(string path) => _requestCounts.TryGetValue(path, out var count) ? count : 0;

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            _ = HandleClientAsync(client, token);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        using (client)
        {
            try
            {
                await using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);

                var requestLine = await reader.ReadLineAsync(token);
                if (string.IsNullOrEmpty(requestLine))
                {
                    return;
                }

                string? headerLine;
                while (!string.IsNullOrEmpty(headerLine = await reader.ReadLineAsync(token)))
                {
                    // drain headers, none are needed for this fixture server
                }

                var path = requestLine.Split(' ') is { Length: > 1 } parts ? parts[1] : "/";
                var basePath = path.Split('?')[0];
                _requestCounts.AddOrUpdate(basePath, 1, (_, count) => count + 1);

                var body = basePath switch
                {
                    "/slow" => await SlowBodyAsync(token),
                    "/other" => "<html lang=\"en\"><head><title>other</title></head><body><h1>other page</h1></body></html>",
                    // lang attribute is present so this fixture itself has no WCAG violations —
                    // this server exists to prove the scanner doesn't navigate to /other, not to test violation reporting.
                    _ => "<html lang=\"en\"><head><title>root</title></head><body><a href=\"/other\">link</a></body></html>",
                };

                var bodyBytes = Encoding.UTF8.GetBytes(body);
                var header =
                    "HTTP/1.1 200 OK\r\n" +
                    "Content-Type: text/html; charset=utf-8\r\n" +
                    $"Content-Length: {bodyBytes.Length}\r\n" +
                    "Connection: close\r\n\r\n";

                await stream.WriteAsync(Encoding.ASCII.GetBytes(header), token);
                await stream.WriteAsync(bodyBytes, token);
            }
            catch (OperationCanceledException)
            {
                // server disposed mid-request; nothing to do
            }
            catch (IOException)
            {
                // client disconnected (e.g. the scanner timed out and gave up); nothing to do
            }
        }
    }

    private async Task<string> SlowBodyAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(SlowDelayMs, token);
        }
        catch (OperationCanceledException)
        {
            // server disposed while delaying; let the caller's catch handle the aborted write
        }
        return "<html lang=\"en\"><head><title>slow</title></head><body><h1>slow page</h1></body></html>";
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
        try
        {
            _acceptLoop.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // best-effort cleanup
        }
        _cts.Dispose();
    }
}
