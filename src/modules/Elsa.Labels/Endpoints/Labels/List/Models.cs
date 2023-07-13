using Elsa.Common.Models;
using Elsa.Labels.Entities;

namespace Elsa.Labels.Endpoints.Labels.List;

public class Request
{
    public int? Page { get; set; }
    public int? PageSize { get; set; }

    public PageArgs ToPageArgs() => PageArgs.FromPage(Page, PageSize);
}

public class Response
{
    public ICollection<Label> Items { get; set; } = default!;
    public long TotalCount { get; set; }
}