namespace Elsa.ActivityDefinitions.Endpoints.ActivityDefinitions.BulkDelete;

public static class SecurityConstants
{
    public static readonly string[] Policies = { "DeleteActivityDefinitions" };
    public static readonly string[] Permissions = { "DeleteActivityDefinitions" };
    public static readonly string[] Roles = { "DefinitionsDeleter" };
}