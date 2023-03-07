using Elsa.Labels.Contracts;
using FastEndpoints;

namespace Elsa.Labels.Endpoints.Labels.Delete;

public class Delete : Endpoint<Request>
{
    private readonly ILabelStore _store;

    public Delete(ILabelStore store) => _store = store;

    public override void Configure()
    {
        Delete("/labels/{id}");
        Policies(Constants.PolicyName);
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var deleted = await _store.DeleteAsync(request.Id, cancellationToken);

        if (deleted)
            await SendNoContentAsync(cancellationToken);
        else
            await SendNotFoundAsync(cancellationToken);
    }
}