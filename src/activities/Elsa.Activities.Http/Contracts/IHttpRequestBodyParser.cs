using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Contracts
{
    public interface IHttpRequestBodyParser
    {
        int Priority { get; }
        string?[] SupportedContentTypes { get; }
        Task<object?> ParseAsync(HttpRequest request, Type? targetType = default, CancellationToken cancellationToken = default);
    }
}