using Elsa.ExternalAuthentication.Endpoints.Previews;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Elsa.ExternalAuthentication.UnitTests.Previews;

public class PreviewNavigationPathTests
{
    [Test]
    [Arguments("", "/external-authentication/connections/connection-1/preview", "/external-authentication/previews/handle%2F1/authorize")]
    [Arguments("", "/elsa/api/external-authentication/connections/connection-1/preview", "/elsa/api/external-authentication/previews/handle%2F1/authorize")]
    [Arguments("/root", "/elsa/api/external-authentication/connections/connection-1/preview", "/root/elsa/api/external-authentication/previews/handle%2F1/authorize")]
    public async Task AuthorizePathPreservesPathBaseAndMappedRoutePrefix(string pathBase, string requestPath, string expected)
    {
        var result = InitiatePreview.BuildAuthorizePath(new PathString(pathBase), new PathString(requestPath), "handle/1");

        await Assert.That(result).IsEqualTo(expected);
    }
}
