using Elsa.Http.Services;
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
}