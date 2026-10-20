using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Elsa.Workflows;

namespace NativeCompensation.E1;

// Experiment-only schema. The persisted value is a JSON map, never a CLR-type alias.
internal sealed class Journal
{
    internal const string PropertyKey = "elsa.experimental.native-compensation.e1";
    public int Schema { get; set; } = 1;
    public long Revision { get; set; }
    public string Seed { get; set; } = Guid.NewGuid().ToString("N");
    public string DefinitionVersion { get; set; } = Program.VersionId;
    public string HandlerVersion { get; set; } = "e1-handler-contract-v1";
    public string Phase { get; set; } = "Forward";
    public string Handoff { get; set; } = "Compensated";
    public string? ProtectedOriginalCause { get; set; }
    public string? ProtectedRecoveryCause { get; set; }
    public int CompletionOrdinal { get; set; }
    public int OutwardCount { get; set; }
    public string OwnerToken { get; set; } = "synthetic-owner-a";
    public bool FaultOwnershipReserved { get; set; }
    public string? FaultedContextId { get; set; }
    public List<Entry> Entries { get; set; } = [];

    internal static Journal Read(IDictionary<string, object> properties)
    {
        if (!properties.TryGetValue(PropertyKey, out var value))
        {
            throw new InvalidOperationException("Journal absent; do not manufacture empty recovery state.");
        }
        var map = value is JsonElement json ? json : JsonSerializer.SerializeToElement(value);
        if (map.GetProperty(nameof(Schema)).GetInt32() != 1)
        {
            throw new InvalidOperationException("Unsupported journal schema; original state must be retained.");
        }
        return map.Deserialize<Journal>() ?? throw new InvalidOperationException("Empty journal.");
    }

    internal void Write(WorkflowExecutionContext context)
    {
        Revision++;
        context.Properties[PropertyKey] = JsonSerializer.SerializeToElement(this);
    }

    internal string Key(int slot, string direction) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"e1/v1/{Seed}/{slot}/{direction}")));
}

internal sealed class Entry
{
    public int Slot { get; set; }
    public string OccurrenceId { get; set; } = "";
    public string OperationKey { get; set; } = "";
    public string UndoKey { get; set; } = "";
    public string Status { get; set; } = "Armed";
    public string ProtectedRequest { get; set; } = "";
    public string? ProtectedResult { get; set; }
    public string? ProtectedUndoReceipt { get; set; }
    public string? ForwardContextId { get; set; }
    public string? UndoContextId { get; set; }
    public string AttemptId { get; set; } = "";
    public int? CompletionOrdinal { get; set; }
}
