using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Extensions
{
    public static class HttpRequestExtensions
    {
        public static async Task<byte[]?> ReadContentAsBytesAsync(
            this HttpRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.Body == null || request.Body == Stream.Null)
                return null;

            request.EnableBuffering();

            var content = await request.Body.ReadBytesToEndAsync(cancellationToken);
            request.Body.Seek(0, SeekOrigin.Begin);
            return content;
        }

        public static async Task<string?> ReadContentAsStringAsync(this HttpRequest request, CancellationToken cancellationToken = default)
        {
            var bytes = await request.ReadContentAsBytesAsync(cancellationToken);
            return bytes != null ? Encoding.UTF8.GetString(bytes) : default;
        }

        public static Uri ToAbsoluteUrl(this HttpRequest request, string relativePath)
        {
            var absoluteUrl = $"{request.Scheme}://{request.Host}{relativePath}";
            return new Uri(absoluteUrl, UriKind.Absolute);
        }

        public static bool TryGetCorrelationId(this HttpRequest request, out string? correlationId)
        {
            if (request.Query.ContainsKey("correlation"))
            {
                correlationId = request.Query["correlation"];
                return true;
            }

            if (request.Headers.ContainsKey("X-Correlation-Id"))
            {
                correlationId = request.Headers["X-Correlation-Id"].ToString();
                return true;
            }

            correlationId = null;
            return false;
        }
        
        public static bool GetUseDispatch(this HttpRequest request)
        {
            if (request.Query.ContainsKey("x-dispatch"))
                return true;

            if (request.Headers.ContainsKey("X-Dispatch"))
                return true;
            
            return false;
        }
    }
}