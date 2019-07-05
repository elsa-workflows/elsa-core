using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Extensions
{
    public static class HttpRequestExtensions
    {
        public static async Task<string> ReadBodyAsync(this HttpRequest request)
        {
            if (request.Body == null || request.Body == Stream.Null)
            {
                return null;
            }
            
            request.EnableBuffering();

            using (var reader = new StreamReader(request.Body))
            {
                var content = await reader.ReadToEndAsync();
                request.Body.Seek(0, SeekOrigin.Begin);
                return content;
            }
        }
        
        public static Uri ToAbsoluteUrl(this HttpRequest request, string relativePath)
        {
            var absoluteUrl = $"{request.Scheme}://{request.Host}{relativePath}";
            return new Uri(absoluteUrl, UriKind.Absolute);
        }
    }
}