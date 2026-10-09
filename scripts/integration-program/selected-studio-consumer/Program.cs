using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Elsa.Studio.Options;
using Elsa.Studio.Services;
using Microsoft.Extensions.Options;

var expected = new Uri("https://example.invalid/elsa/api");
var accessor = new DefaultRemoteBackendAccessor(Options.Create(new BackendOptions { Url = expected }));
if (accessor.RemoteBackend.Url != expected)
{
    throw new InvalidOperationException("Configured backend URI was not preserved.");
}

Console.WriteLine("SELECTED_CONSUMER_PROOF=" + JsonSerializer.Serialize(new
{
    backendUriPreserved = true,
    loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
        .Where(assembly => assembly.GetName().Name?.StartsWith("Elsa", StringComparison.Ordinal) == true)
        .OrderBy(assembly => assembly.FullName, StringComparer.Ordinal)
        .Select(assembly => new
        {
            name = assembly.GetName().Name,
            fullName = assembly.FullName,
            version = assembly.GetName().Version?.ToString(),
            informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            location = assembly.Location,
            sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location))).ToLowerInvariant()
        }).ToArray()
}));
