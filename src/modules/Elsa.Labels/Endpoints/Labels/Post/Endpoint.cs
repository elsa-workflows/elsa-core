using Elsa.Labels.Contracts;
using Elsa.Workflows.Core.Contracts;
using FastEndpoints;
using JetBrains.Annotations;

namespace Elsa.Labels.Endpoints.Labels.Post;

[PublicAPI]
internal class Create : Endpoint<Request, Response, LabelMapper>
{
    private readonly ILabelStore _store;

    public Create(ILabelStore store, IIdentityGenerator identityGenerator)
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
        var response = Map.FromEntity(label);
        await SendCreatedAtAsync<Get.Get>(new { Id = label.Id }, response, cancellation: cancellationToken);
    }
}