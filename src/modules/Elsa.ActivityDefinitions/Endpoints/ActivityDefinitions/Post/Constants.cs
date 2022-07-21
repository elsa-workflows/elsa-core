namespace Elsa.ActivityDefinitions.Endpoints.ActivityDefinitions.Post;

public static class SecurityConstants
{
    public static readonly string[] Policies = { "CreateActivityDefinitions", "UpdateActivityDefinitions" };
    public static readonly string[] Permissions = { "CreateActivityDefinitions", "UpdateActivityDefinitions" };
    public static readonly string[] Roles = { "ActivityDefinitionsCreator", "ActivityDefinitionsUpdater" };
}