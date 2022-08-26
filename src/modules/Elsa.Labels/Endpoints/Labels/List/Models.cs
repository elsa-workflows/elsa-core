using Elsa.Labels.Entities;
using Elsa.Models;

namespace Elsa.Labels.Endpoints.Labels.List;

public class Request
{
    public int? Page { get; set; }
    public int? PageSize { get; set; }

    public PageArgs ToPageArgs() => new(Page, PageSize);
}

public class Response
{
    public ICollection<Label> Items { get; set; } = default!;
    public long TotalCount { get; set; }
}