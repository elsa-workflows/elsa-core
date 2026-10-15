if (args.Length != 1 || !Uri.TryCreate(args[0], UriKind.Absolute, out var uri))
{
    Console.Error.WriteLine("Usage: TlsSmoke <absolute-url>");
    return 2;
}

using var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(30)
};

using var response = await httpClient.GetAsync(uri);
response.EnsureSuccessStatusCode();

Console.WriteLine($"TLS request to {uri} returned {(int)response.StatusCode}.");
return 0;
