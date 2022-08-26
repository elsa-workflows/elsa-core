using Elsa.Expressions.Helpers;
using Elsa.Labels.Entities;
using Elsa.Workflows.Core.Services;
using FastEndpoints;

namespace Elsa.Labels.Endpoints.Labels.Post;

public class LabelMapper : Mapper<Request, Response, Label>
{
    public override Label ToEntity(Request request) => new()
    {
        Id = Resolve<IIdentityGenerator>().GenerateId(),
        Name = request.Name.Trim(),
        Description = request.Description?.Trim(),
        Color = request.Color?.Trim()
    };

    public override Task<Response> FromEntityAsync(Label e) => Task.FromResult(new Response
    {
        Id = e.Id,
        Color = e.Color,
        Description = e.Description,
        NormalizedName = e.NormalizedName
    });
}