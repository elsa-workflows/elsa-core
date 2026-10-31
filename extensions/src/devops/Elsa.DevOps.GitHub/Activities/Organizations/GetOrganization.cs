using Elsa.DevOps.GitHub.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities.Organizations;

/// <summary>
/// Retrieves details of a GitHub organization.
/// </summary>
[Activity(
    "Elsa.GitHub.Organizations",
    "GitHub Organizations",
    "Retrieves details of a GitHub organization.",
    DisplayName = "Get Organization")]
[UsedImplicitly]
public class GetOrganization : GitHubActivity
{
    /// <summary>
    /// The organization name.
    /// </summary>
    [Input(Description = "The organization name.")]
    public Input<string> OrganizationName { get; set; } = null!;

    /// <summary>
    /// The retrieved organization.
    /// </summary>
    [Output(Description = "The retrieved organization.")]
    public Output<Organization> RetrievedOrganization { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var organizationName = context.Get(OrganizationName)!;

        var client = GetClient(context);
        var organization = await client.Organization.Get(organizationName);

        context.Set(RetrievedOrganization, organization);
    }
}