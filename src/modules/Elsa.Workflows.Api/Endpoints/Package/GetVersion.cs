using System.Reflection;
using Elsa.Abstractions;

namespace Elsa.Workflows.Api.Endpoints.Package;

internal class GetVersion : ElsaEndpoint<Request, string>
{
    private const string DefaultVersion = "3.0.0";
    
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/package/version");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var versionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        var version = versionAttribute?.InformationalVersion;
        
        version ??= DefaultVersion;
        
        await SendOkAsync(version, cancellationToken);
    }
}

internal class Request {}