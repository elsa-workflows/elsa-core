using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.EventPipe;

// Private transport only. Python verifies ownership, paths, archive bytes and policy.
// Buffer all results: a malformed/truncated later trace must never yield a usable prefix.
var traces = new List<object>();
var records = new List<object>();
try
{
    if (args.Length == 0)
        throw new ArgumentException();
    foreach (var path in args)
    {
        var digest = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
        using var source = new EventPipeEventSource(path);
        source.Clr.AssemblyLoaderStop += entry =>
        {
            object? Payload(string field) => entry.PayloadNames.Contains(field) ? entry.PayloadByName(field) : null;
            string? Text(string field) => Payload(field)?.ToString();
            var name = Text("ResultAssemblyName") ?? Text("AssemblyName");
            var simple = name?.Split(',', 2)[0];
            if (simple is not ("Microsoft.NET.Sdk.WebAssembly.Pack.Tasks" or "Microsoft.NET.WebAssembly.Webcil"))
                return;
            if (records.Count >= 2000)
                throw new InvalidDataException();
            records.Add(new
            {
                trace = Path.GetFileName(path), trace_sha256 = digest, pid = entry.ProcessID,
                event_id = (int)entry.ID, provider = entry.ProviderName, event_name = entry.EventName,
                success = Payload("Success") is bool value ? (bool?)value : null,
                assembly = Text("AssemblyName"), result = Text("ResultAssemblyName"),
                path = Text("ResultAssemblyPath"), requested_path = Text("AssemblyPath"),
                requestor = Text("RequestingAssembly"), context = Text("AssemblyLoadContext"),
                requestor_context = Text("RequestingAssemblyLoadContext")
            });
        };
        source.Process();
        traces.Add(new { file = Path.GetFileName(path), sha256 = digest, events_lost = source.EventsLost, parser_completed = true });
    }
    Console.WriteLine(JsonSerializer.Serialize(new { schema = 1, traces, records }));
    return 0;
}
catch
{
    // No exception messages, paths or partial records enter stdout/stderr on failure.
    Console.Error.WriteLine("converter_trace_decode_failed");
    return 1;
}
