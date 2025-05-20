using Elsa.Abstractions;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.ActivityDescriptorOptions.Get;

[PublicAPI]
internal class Get : ElsaEndpoint<Request, Response>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IActivityRegistryLookupService _registryLookup;
    private readonly IPropertyUIHandlerResolver _optionsResolver;

    /// <inheritdoc />
    public Get(IServiceProvider serviceProvider, IActivityRegistryLookupService registryLookup, IPropertyUIHandlerResolver optionsResolver)
    {
        _serviceProvider = serviceProvider;
        _registryLookup = registryLookup;
        _optionsResolver = optionsResolver;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/descriptors/activities/{activityTypeName}/options/{propertyName}");
        ConfigurePermissions("read:*", "read:activity-descriptors-options");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var descriptor = request.Version == null ? await _registryLookup.FindAsync(request.ActivityTypeName) : await _registryLookup.FindAsync(request.ActivityTypeName, request.Version.Value);
        if (descriptor == null)
            await SendNotFoundAsync(cancellationToken);

        var propertyDescriptor =  descriptor!.Inputs.FirstOrDefault(x => x.Name == request.PropertyName);

        if(propertyDescriptor == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }
        
        var inputOptions = await _optionsResolver.GetUIPropertiesAsync(propertyDescriptor.PropertyInfo!, request.Context, cancellationToken);

        await SendOkAsync(new Response(inputOptions), cancellationToken);
    }
}