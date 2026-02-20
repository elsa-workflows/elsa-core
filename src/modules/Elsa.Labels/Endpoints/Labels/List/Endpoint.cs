using Elsa.Abstractions;
using Elsa.Labels.Contracts;
using FastEndpoints;

namespace Elsa.Labels.Endpoints.Labels.List;

public class List : ElsaEndpoint<Request, Response, PageMapper>
{
    private readonly ILabelStore _store;

    public List(ILabelStore store)
    {
        _store = store;
    }

    public override void Configure()
    {
        Get("/labels");
        ConfigurePermissions("read:labels");
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var pageOfLabels = await _store.ListAsync(request.ToPageArgs(), cancellationToken);
        return Map.FromEntity(pageOfLabels);
    }
}