using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Organizations;

/// <summary>
/// Lists members of a GitHub organization.
/// </summary>
[Activity(
    "Elsa.GitHub.Organizations",
    "GitHub Organizations",
    "Lists members of a GitHub organization.",
    DisplayName = "Search Organization Members")]
[UsedImplicitly]
public class SearchOrganizationMembers : GitHubActivity
{
    /// <summary>
    /// The organization name.
    /// </summary>
    [Input(Description = "The organization name.")]
    public Input<string> OrganizationName { get; set; } = null!;

    /// <summary>
    /// Optional filter to search for specific member names (case-insensitive partial match).
    /// </summary>
    [Input(Description = "Optional filter to search for specific member names (case-insensitive partial match).")]
    public Input<string?> Filter { get; set; } = null!;

    /// <summary>
    /// The page number to retrieve.
    /// </summary>
    [Input(Description = "The page number to retrieve.")]
    public Input<int?> Page { get; set; } = null!;

    /// <summary>
    /// The number of records per page.
    /// </summary>
    [Input(Description = "The number of records per page.")]
    public Input<int?> PageSize { get; set; } = null!;

    /// <summary>
    /// The membership role filter.
    /// </summary>
    [Input(Description = "The membership role filter (all, admin, member).")]
    public Input<string?> Role { get; set; } = null!;

    /// <summary>
    /// List of organization members.
    /// </summary>
    [Output(Description = "List of organization members.")]
    public Output<IReadOnlyList<User>> Members { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var organizationName = context.Get(OrganizationName)!;
        var filter = context.Get(Filter);
        var page = context.Get(Page);
        var pageSize = context.Get(PageSize);
        var role = context.Get(Role);

        var client = GetClient(context);

        var options = new ApiOptions();
        if (page.HasValue)
            options.StartPage = page.Value;

        if (pageSize.HasValue)
            options.PageSize = pageSize.Value;

        var members = await client.Organization.Member.GetAll(
            organizationName,
            OrganizationMembersFilter.All,
            !string.IsNullOrEmpty(role) ? ParseRole(role) : OrganizationMembersRole.All,
            options
        );

        // Apply filter if specified
        if (!string.IsNullOrEmpty(filter))
        {
            members = members
                .Where(m => m.Login.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        context.Set(Members, members);
    }

    private static OrganizationMembersRole ParseRole(string role)
    {
        return role.ToLowerInvariant() switch
        {
            "admin" => OrganizationMembersRole.Admin,
            "member" => OrganizationMembersRole.Member,
            _ => OrganizationMembersRole.All
        };
    }
}