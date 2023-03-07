using Elsa.Labels.Contracts;
using FastEndpoints;

namespace Elsa.Labels.Endpoints.Labels.Get;

internal class Get : Endpoint<Request, Response, LabelMapper>
{
    private readonly ILabelStore _store;

    public Get(ILabelStore store)
    {
        _store = store;
    }

    public override void Configure()
    {
        Get("/labels/{id}");
        Policies(Constants.PolicyName);
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var label = await _store.FindByIdAsync(request.Id, cancellationToken);

        if (label == null)
            await SendNotFoundAsync(cancellationToken);
        else
        {
            var response = Map.FromEntity(label);
            await SendOkAsync(response, cancellationToken);
        }
    }
}