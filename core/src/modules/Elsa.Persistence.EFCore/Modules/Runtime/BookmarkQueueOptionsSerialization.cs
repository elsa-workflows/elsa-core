using System.Reflection;
using System.Text.Json;
using Elsa.Workflows;
using Elsa.Workflows.Runtime.Options;

namespace Elsa.Persistence.EFCore.Modules.Runtime;

internal static class BookmarkQueueOptionsSerialization
{
    public static (ResumeBookmarkOptions? Options, string? UnreadableOptions) Read(IPayloadSerializer serializer, string? json)
    {
        if (string.IsNullOrEmpty(json))
            return (null, null);

        try
        {
            return (serializer.Deserialize<ResumeBookmarkOptions>(json), null);
        }
        catch (Exception exception) when (exception is JsonException or Newtonsoft.Json.JsonException or
                                          InvalidOperationException or ArgumentException or KeyNotFoundException or
                                          NotSupportedException or FormatException or TargetInvocationException)
        {
            return (null, json);
        }
    }

    public static string? Write(IPayloadSerializer serializer, ResumeBookmarkOptions? options, string? unreadableOptions) =>
        unreadableOptions ?? (options != null ? serializer.Serialize(options) : null);
}
