using Elsa.ExternalAuthentication.Endpoints.Previews;
using Microsoft.AspNetCore.Http;

namespace Elsa.ExternalAuthentication.UnitTests.Previews;

public class PreviewNavigationPathTests
{
    [Theory]
    [InlineData("", "/external-authentication/connections/connection-1/preview", "/external-authentication/previews/handle%2F1/authorize")]
    [InlineData("", "/elsa/api/external-authentication/connections/connection-1/preview", "/elsa/api/external-authentication/previews/handle%2F1/authorize")]
    [InlineData("/root", "/elsa/api/external-authentication/connections/connection-1/preview", "/root/elsa/api/external-authentication/previews/handle%2F1/authorize")]
    public void AuthorizePathPreservesPathBaseAndMappedRoutePrefix(string pathBase, string requestPath, string expected)
    {
        var result = InitiatePreview.BuildAuthorizePath(new PathString(pathBase), new PathString(requestPath), "handle/1");

        Assert.Equal(expected, result);
    }
}
