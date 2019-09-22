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
        public static async Task<byte[]> ReadContentAsBytesAsync(
            this HttpRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.Body == null || request.Body == Stream.Null)
            {
                return null;
            }

            request.EnableBuffering();

            var content = await request.Body.ReadBytesToEndAsync(cancellationToken);
            request.Body.Seek(0, SeekOrigin.Begin);
            return content;
        }

        public static async Task<string> ReadContentAsStringAsync(
            this HttpRequest request,
            CancellationToken cancellationToken = default)
        {
            var bytes = await request.ReadContentAsBytesAsync(cancellationToken);
            return Encoding.UTF8.GetString(bytes);
        }

        public static Uri ToAbsoluteUrl(this HttpRequest request, string relativePath)
        {
            var absoluteUrl = $"{request.Scheme}://{request.Host}{relativePath}";
            return new Uri(absoluteUrl, UriKind.Absolute);
        }
    }
}