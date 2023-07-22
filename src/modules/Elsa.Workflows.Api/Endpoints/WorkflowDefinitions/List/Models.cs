using Elsa.Common.Entities;
using Elsa.Workflows.Api.Models;
using FastEndpoints;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.List;

internal class Request
{
    public string? VersionOptions { get; set; }
    public string[]? DefinitionIds { get; set; }
    public string[]? Ids { get; set; }
    [BindFrom("materializer")] public string? MaterializerName { get; set; }
    [BindFrom("label")] public string[]? Labels { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public OrderByWorkflowDefinition? OrderBy { get; set; }
    public OrderDirection? OrderDirection { get; set; }
    public string? SearchTerm { get; set; }
}