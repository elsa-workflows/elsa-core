using Elsa.Labels.Services;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using FastEndpoints;

namespace Elsa.Labels.Endpoints.Labels.Post;

public class Create : Endpoint<Request, Response, LabelMapper>
{
    private readonly ILabelStore _store;

    public Create(ILabelStore store, IIdentityGenerator identityGenerator, SerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
    }

    public override void Configure()
    {
        Post("/labels");
        Policies(Constants.PolicyName);
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var label = Map.ToEntity(request);
        await _store.SaveAsync(label, cancellationToken);
        var response = await Map.FromEntityAsync(label);
        await SendCreatedAtAsync<Get.Get>(new { Id = label.Id }, response, cancellation: cancellationToken);
    }
}