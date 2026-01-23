using Elsa.Abstractions;
using Elsa.Labels.Contracts;
using FastEndpoints;

namespace Elsa.Labels.Endpoints.Labels.Get;

internal class Get : ElsaEndpoint<Request, Response, LabelMapper>
{
    private readonly ILabelStore _store;

    public Get(ILabelStore store)
    {
        _store = store;
    }

    public override void Configure()
    {
        Get("/labels/{id}");
        ConfigurePermissions("read:labels");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var label = await _store.FindByIdAsync(request.Id, cancellationToken);

        if (label == null)
            await Send.NotFoundAsync(cancellationToken);
        else
        {
            var response = Map.FromEntity(label);
            await Send.OkAsync(response, cancellationToken);
        }
    }
}