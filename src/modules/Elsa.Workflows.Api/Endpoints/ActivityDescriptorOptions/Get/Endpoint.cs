using Elsa.Abstractions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.ActivityDescriptorOptions.Get;

[PublicAPI]
internal class Get : ElsaEndpoint<Request, Response>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IActivityRegistry _registry;
    private readonly IPropertyOptionsResolver _optionsResolver;

    /// <inheritdoc />
    public Get(IServiceProvider serviceProvider, IActivityRegistry registry, IPropertyOptionsResolver optionsResolver)
    {
        _serviceProvider = serviceProvider;
        _registry = registry;
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
        var descriptor = request.Version == null ? _registry.Find(request.ActivityTypeName) : _registry.Find(request.ActivityTypeName, request.Version.Value);
        if (descriptor == null)
            await SendNotFoundAsync(cancellationToken);

        var propertyDescriptor =  descriptor!.Inputs.FirstOrDefault(x => x.Name == request.PropertyName);

        if(propertyDescriptor == null)
            await SendNotFoundAsync(cancellationToken);

        var optionsResolver = new PropertyOptionsResolver(_serviceProvider);

        var inputOptions = await optionsResolver.GetOptionsAsync(propertyDescriptor!.PropertyInfo, request.Context, cancellationToken);

        await SendOkAsync(new Response(inputOptions.OptionsItems), cancellationToken);
    }
}