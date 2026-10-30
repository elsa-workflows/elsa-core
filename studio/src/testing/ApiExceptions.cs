using System.Net;
using Refit;

namespace Elsa.Studio.Testing;

/// <summary>Builds the <see cref="ApiException"/> Refit raises for a failed backend response.</summary>
internal static class ApiExceptions
{
    public static ApiException Create(HttpStatusCode statusCode, string? content = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://elsa.example.test/");
        using var response = new HttpResponseMessage(statusCode) { Content = new StringContent(content ?? string.Empty) };
        return ApiException.Create(request, HttpMethod.Get, response, new RefitSettings()).GetAwaiter().GetResult();
    }
}
