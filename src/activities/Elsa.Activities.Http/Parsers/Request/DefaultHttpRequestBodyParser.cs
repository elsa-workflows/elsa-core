using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Contracts;
using Elsa.Activities.Http.Extensions;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Parsers.Request
{
    public class DefaultHttpRequestBodyParser : IHttpRequestBodyParser
    {
        public int Priority => -1;
        public string?[] SupportedContentTypes => new[] { "", default };

        public async Task<object?> ParseAsync(HttpRequest request, Type? targetType = default, CancellationToken cancellationToken = default) => await request.ReadContentAsStringAsync(cancellationToken);
    }
}