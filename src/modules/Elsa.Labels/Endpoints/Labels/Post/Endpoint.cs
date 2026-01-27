using Elsa.Abstractions;
using Elsa.Labels.Contracts;
using Elsa.Workflows;
using FastEndpoints;
using JetBrains.Annotations;

namespace Elsa.Labels.Endpoints.Labels.Post;

[PublicAPI]
internal class Create : ElsaEndpoint<Request, Response, LabelMapper>
{
    private readonly ILabelStore _store;

    public Create(ILabelStore store, IIdentityGenerator identityGenerator)
    {
        _store = store;
    }

    public override void Configure()
    {
        Post("/labels");
        ConfigurePermissions("create:labels");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var label = Map.ToEntity(request);
        await _store.SaveAsync(label, cancellationToken);
        var response = Map.FromEntity(label);
        await Send.CreatedAtAsync<Get.Get>(new { Id = label.Id }, response, cancellation: cancellationToken);
    }
}