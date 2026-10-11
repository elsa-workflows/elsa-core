using System.Text.Json;

using NuGetUpdater.Core;
using NuGetUpdater.Core.Discover;

// Temporary #8636 diagnostic. Remove after native inventory acceptance is retained.
// No hosted job dictionary is supplied: both default experiments are diagnostic only.
var workspaces = new[] { "/", "/extensions/src/Elsa.Testing.Extensions" };
var statuses = new List<bool>();
for (var index = 0; index < workspaces.Length; index++)
{
    Console.WriteLine($"ELSA8636_WORKSPACE_{index}_STARTED");
    try
    {
        // Separate instances prevent state from a failed first workspace affecting the second.
        var worker = new DiscoveryWorker("8636-diagnostic", new ExperimentsManager(), new ConsoleLogger());
        await worker.RunAsync(args[0], workspaces[index], Path.Combine(args[1], $"workspace-{index}.json"));
        Console.WriteLine($"ELSA8636_WORKSPACE_{index}_RETURNED");
        statuses.Add(true);
    }
    catch (Exception exception)
    {
        // This raw exception and native output remain ephemeral; the artifact omits messages.
        Console.Error.WriteLine(exception);
        Console.WriteLine($"ELSA8636_WORKSPACE_{index}_THREW");
        statuses.Add(false);
    }
}
await File.WriteAllTextAsync(Path.Combine(args[1], "calls.json"), JsonSerializer.Serialize(statuses));
return statuses.All(completed => completed) ? 0 : 1;
