using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Labels.Contracts;
using FastEndpoints;

namespace Elsa.Labels.Endpoints.Labels.Delete;

public class Delete : ElsaEndpoint<Request>
{
    private readonly ILabelStore _store;

    public Delete(ILabelStore store) => _store = store;

    public override void Configure()
    {
        Delete("/labels/{id}");
        RequirePermission(Elsa.Labels.Permissions.LabelPermissions.Labels, CoreVerbs.Delete);
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var deleted = await _store.DeleteAsync(request.Id, cancellationToken);

        if (deleted)
            await Send.NoContentAsync(cancellationToken);
        else
            await Send.NotFoundAsync(cancellationToken);
    }
}