using Elsa.Labels.Entities;
using FastEndpoints;

namespace Elsa.Labels.Endpoints.Labels.Get;

internal class LabelMapper : Mapper<Request, Response, Label>
{
    public override Response FromEntity(Label e) => new()
    {
        Id = e.Id,
        Color = e.Color,
        Description = e.Description,
        NormalizedName = e.NormalizedName
    };
}