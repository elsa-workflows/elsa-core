using System.Net;
using Refit;

namespace Elsa.Studio.Testing;

/// <summary>Builds the <see cref="ApiException"/> Refit raises for a failed backend response.</summary>
internal static class ApiExceptions
{
    public static ApiException Create(HttpStatusCode statusCode, string? content = null) =>
        ApiException.Create(
            new HttpRequestMessage(HttpMethod.Get, "https://elsa.example.test/"),
            HttpMethod.Get,
            new HttpResponseMessage(statusCode) { Content = new StringContent(content ?? string.Empty) },
            new RefitSettings()).GetAwaiter().GetResult();
}
