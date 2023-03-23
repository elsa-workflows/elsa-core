using Elsa.Labels.Entities;
using Elsa.Workflows.Core.Contracts;
using FastEndpoints;

namespace Elsa.Labels.Endpoints.Labels.Post;

internal class LabelMapper : Mapper<Request, Response, Label>
{
    public override Label ToEntity(Request request) => new()
    {
        Id = Resolve<IIdentityGenerator>().GenerateId(),
        Name = request.Name.Trim(),
        Description = request.Description?.Trim(),
        Color = request.Color?.Trim()
    };

    public override Response FromEntity(Label e) => new()
    {
        Id = e.Id,
        Color = e.Color,
        Description = e.Description,
        NormalizedName = e.NormalizedName
    };
}