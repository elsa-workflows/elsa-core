using Elsa.Labels.Entities;
using FastEndpoints;

namespace Elsa.Labels.Endpoints.Labels.Update;

internal class LabelMapper : Mapper<Request, Response, Label>
{
    public override Label ToEntity(Request request) => new()
    {
        Id = request.Id,
        Name = request.Name.Trim(),
        Description = request.Description?.Trim(),
        Color = request.Color?.Trim()
    };

    public override Response FromEntity(Label e) => new Response
    {
        Id = e.Id,
        Color = e.Color,
        Description = e.Description,
        NormalizedName = e.NormalizedName
    };

    public override Label UpdateEntity(Request r, Label e)
    {
        e.Color = r.Color?.Trim();
        e.Description = r.Description?.Trim();
        e.Name = r.Name.Trim();
        e.NormalizedName = r.Name.ToLowerInvariant();
        return e;
    }
}