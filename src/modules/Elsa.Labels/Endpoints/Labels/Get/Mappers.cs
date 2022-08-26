using Elsa.Labels.Entities;
using Elsa.Workflows.Core.Services;
using FastEndpoints;

namespace Elsa.Labels.Endpoints.Labels.Get;

public class LabelMapper : Mapper<Request, Response, Label>
{
    public override Task<Response> FromEntityAsync(Label e) => Task.FromResult(new Response
    {
        Id = e.Id,
        Color = e.Color,
        Description = e.Description,
        NormalizedName = e.NormalizedName
    });
}