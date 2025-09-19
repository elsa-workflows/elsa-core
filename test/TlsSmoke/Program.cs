using System.Net.Http;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: TlsSmoke <url>");
    return 1;
}

var url = args[0];

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
using var client = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(10)
};

try
{
    using var response = await client.GetAsync(url, cts.Token);
    response.EnsureSuccessStatusCode();
    Console.WriteLine($"Successfully fetched {url} with status {(int)response.StatusCode}.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Request to {url} failed: {ex.Message}");
    return 1;
}
