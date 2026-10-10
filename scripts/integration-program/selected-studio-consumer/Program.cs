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
    loadedAssemblies = SelectedAssemblyProof.Snapshot()
}));
