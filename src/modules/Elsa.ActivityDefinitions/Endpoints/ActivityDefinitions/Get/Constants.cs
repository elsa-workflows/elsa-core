namespace Elsa.ActivityDefinitions.Endpoints.ActivityDefinitions.Get;

public static class SecurityConstants
{
    public static readonly string[] Policies = { "ReadActivityDefinitions" };
    public static readonly string[] Permissions = { "ReadActivityDefinitions" };
    public static readonly string[] Roles = { "ActivityDefinitionsReader" };
}