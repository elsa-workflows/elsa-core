using Elsa.Abstractions;
using Elsa.Labels.Contracts;
using FastEndpoints;
using JetBrains.Annotations;

namespace Elsa.Labels.Endpoints.Labels.Update;

[PublicAPI]
internal class Update(ILabelStore store) : ElsaEndpoint<Request, Response, LabelMapper>
{
    public override void Configure()
    {
        Post("/labels/{id}");
        ConfigurePermissions("update:labels");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var label = await store.FindByIdAsync(request.Id, cancellationToken);

        if (label == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        label = Map.UpdateEntity(request, label);

        await store.SaveAsync(label, cancellationToken);
        var response = Map.FromEntity(label);
        await Send.OkAsync(response, cancellationToken);
    }
}