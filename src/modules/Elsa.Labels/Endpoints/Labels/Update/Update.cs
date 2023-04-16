using Elsa.Labels.Contracts;
using FastEndpoints;
using JetBrains.Annotations;

namespace Elsa.Labels.Endpoints.Labels.Update;

[PublicAPI]
internal class Update : Endpoint<Request, Response, LabelMapper>
{
    private readonly ILabelStore _store;

    public Update(ILabelStore store)
    {
        _store = store;
    }

    public override void Configure()
    {
        Post("/labels/{id}");
        Policies(Constants.PolicyName);
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var label = await _store.FindByIdAsync(request.Id, cancellationToken);

        if (label == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        label = Map.UpdateEntity(request, label);

        await _store.SaveAsync(label, cancellationToken);
        var response = Map.FromEntity(label);
        await SendOkAsync(response, cancellationToken);
    }
}