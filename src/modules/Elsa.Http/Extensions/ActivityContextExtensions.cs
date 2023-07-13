using Elsa.Http.Contracts;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

internal static class ActivityContextExtensions
{
    public static async Task<object?> ParseContentAsync(this ActivityExecutionContext context, Stream content, string contentType, Type? returnType, CancellationToken cancellationToken)
    {
        var parsers = context.GetServices<IHttpContentParser>().OrderByDescending(x => x.Priority).ToList();
        var contentParser = parsers.First(x => x.GetSupportsContentType(contentType));
        return await contentParser.ReadAsync(content, returnType, cancellationToken);
    }

    public static IEnumerable<KeyValuePair<string, string[]>> GetHeaders(this ActivityExecutionContext context, Input input)
    {
        var value = context.Get(input.MemoryBlockReference());

        if (value is IDictionary<string, string[]> dictionary1)
            return dictionary1;

        if (value is IDictionary<string, string> dictionary2)
            return dictionary2.ToDictionary(x => x.Key, x => new[] { x.Value });

        if (value is IDictionary<string, object> dictionary3)
            return dictionary3.ToDictionary(
                pair => pair.Key,
                pair => pair.Value is ICollection<object> collection
                    ? collection.Select(x => x.ToString()!).ToArray()
                    : new[] { pair.Value.ToString()! });

        return Array.Empty<KeyValuePair<string, string[]>>();
    }
}