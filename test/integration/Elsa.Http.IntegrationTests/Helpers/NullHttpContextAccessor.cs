using Microsoft.AspNetCore.Http;

namespace Elsa.Http.IntegrationTests.Helpers;

/// <summary>
/// A null HTTP context accessor that always returns null.
/// Used for testing scenarios where HTTP context is lost.
/// </summary>
internal class NullHttpContextAccessor : IHttpContextAccessor
{
    public HttpContext? HttpContext { get; set; } = null;
}

