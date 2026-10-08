using Elsa.Common.Models;
using Elsa.Labels.Entities;
using FastEndpoints;

namespace Elsa.Labels.Endpoints.Labels.List;

public class PageMapper : Mapper<Request, Response, Page<Label>>
{
    public override Response FromEntity(Page<Label> e) => new()
    {
        Items = e.Items,
        TotalCount = e.TotalCount
    };
}