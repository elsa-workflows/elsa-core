namespace Elsa.Expressions.JavaScript.Endpoints.TypeDefinitions;

internal record Request(string WorkflowDefinitionId, string? ActivityTypeName, string? PropertyName)
{
    public Request() : this(default!, default!, default)
    {
    }
}