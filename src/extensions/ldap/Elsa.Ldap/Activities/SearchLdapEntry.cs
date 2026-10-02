using System.DirectoryServices.Protocols;
using Elsa.Ldap.Contracts;
using Elsa.Ldap.Extensions;
using Elsa.Ldap.UIHints;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using Microsoft.Extensions.Logging;

namespace Elsa.Ldap.Activities;

/// <summary>
/// Searches an LDAP directory for a single entry matching the given filter.
/// </summary>
[Activity(
    Namespace = "Elsa",
    Category = "LDAP",
    DisplayName = "Search single LDAP entry",
    Description = "Search for single entry in LDAP directory.",
    Kind = ActivityKind.Task)]
[FlowNode(OutcomeFound, OutcomeNotFound)]
public class SearchLdapEntry : Activity<SearchResultEntry?>
{
    private const string OutcomeFound = "Found";
    private const string OutcomeNotFound = "Not Found";

    [Input(
        DisplayName = "Connection Name",
        Description = "The name of the LDAP connection to use, as configured in the module options. Defaults to 'Default'.",
        UIHandler = typeof(LdapConnectionDropdownOptionsProvider),
        UIHint = InputUIHints.DropDown)]
    public Input<string?> ConnectionName { get; set; } = default!;

    [Input(
        DisplayName = "Base DN",
        Description = "The base distinguished name for the search.")]
    public Input<string> BaseDn { get; set; } = default!;

    [Input(
        DisplayName = "Filter",
        Description = "The LDAP search filter.")]
    public Input<string> Filter { get; set; } = default!;

    [Input(
        DisplayName = "Scope",
        Description = "The scope of the LDAP search.")]
    public Input<SearchScope> Scope { get; set; } = new(SearchScope.Base);

    [Input(
        DisplayName = "Attributes",
        Description = "The attributes to return. Leave empty to return all attributes.")]
    public Input<string[]?> Attributes { get; set; } = default!;

    [Output(
        DisplayName = "Search Result",
        Description = "The search result in serializable form.")]
    public Output<Dictionary<string, string[]>> SearchResult { get; set; } = default!;

    /// <inheritdoc/>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var logger = context.GetRequiredService<ILogger<SearchLdapEntry>>();
        var ldapConnectionFactory = context.GetRequiredService<ILdapConnectionFactory>();

        var connectionName = context.Get(ConnectionName);
        var baseDn = context.Get(BaseDn)!;
        var filter = context.Get(Filter)!;
        var scope = context.Get(Scope);
        var attributes = context.Get(Attributes);

        using var connection = ldapConnectionFactory.CreateConnection(connectionName);

        var request = new SearchRequest(baseDn, filter, scope, attributes);
        request.SizeLimit = 1;

        var response = await connection.SendRequestAsync(request);

        if (response.ResultCode.IsError())
        {
            logger.LogError("{Status} - LDAP request (search entry) failed: {Message}", response.ResultCode, response.ErrorMessage);
        }

        var entry = response.Entries.Count > 0
            ? response.Entries[0]
            : null;

        context.Set(Result, entry);
        context.Set(SearchResult, entry?.MapToAttributesDictionary());

        context.JournalData.Add("ResultCode", response.ResultCode);
        context.JournalData.Add("MatchFound", entry is not null);

        await context.CompleteActivityWithOutcomesAsync(entry is not null ? OutcomeFound : OutcomeNotFound);
    }
}