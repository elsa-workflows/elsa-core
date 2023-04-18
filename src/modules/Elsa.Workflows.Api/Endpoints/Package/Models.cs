namespace Elsa.Workflows.Api.Endpoints.Package;

public class Response
{
    public Response(string packageVersion)
    {
        PackageVersion = packageVersion;
    }

    public string PackageVersion { get; set; }
}

public class Request {}