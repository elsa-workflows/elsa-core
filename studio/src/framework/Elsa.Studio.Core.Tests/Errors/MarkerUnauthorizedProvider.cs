using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Core.Tests.Errors;

/// <summary>Stands in for the host's sign-in hand-off, rendering a marker the tests can find.</summary>
internal sealed class MarkerUnauthorizedProvider : IUnauthorizedComponentProvider
{
    public const string Selector = "#sign-in-redirect";

    public RenderFragment GetUnauthorizedComponent() => builder => builder.AddMarkupContent(0, "<div id=\"sign-in-redirect\"></div>");
}
