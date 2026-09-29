using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Studio.AI.Models;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(WeaverCapabilities))]
[JsonSerializable(typeof(WeaverChatRequest))]
[JsonSerializable(typeof(WeaverStreamEvent))]
public partial class WeaverJsonContext : JsonSerializerContext;
