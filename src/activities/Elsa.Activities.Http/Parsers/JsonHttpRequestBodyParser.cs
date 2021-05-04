using System;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Http.Services;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Parsers
{
    public class JsonHttpRequestBodyParser : IHttpRequestBodyParser
    {
        private readonly IContentSerializer _serializer;

        public JsonHttpRequestBodyParser(IContentSerializer serializer)
        {
            _serializer = serializer;
        }
        
        public int Priority => 0;
        public string?[] SupportedContentTypes => new[] { "application/json", "text/json" };
        
        public async Task<object?> ParseAsync(HttpRequest request, Type? targetType = default, CancellationToken cancellationToken = default)
        {
            var json = await request.ReadContentAsStringAsync(cancellationToken);

            if (json == null)
                return default;
            
            targetType ??= typeof(ExpandoObject);
            return _serializer.Deserialize(json, targetType)!;
        }
    }
}