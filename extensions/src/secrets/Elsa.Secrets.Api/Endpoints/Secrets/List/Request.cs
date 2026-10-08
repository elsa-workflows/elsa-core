namespace Elsa.Secrets.Api.Endpoints.Secrets.List;

internal class Request
{
    public string? SearchTerm { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}