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
/// Searches an LDAP directory for all entries matching the given filter.
/// </summary>
[Activity(
    Namespace = "Elsa",
    Category = "LDAP",
    DisplayName = "Search all LDAP entries",
    Description = "Search for all matching entries in LDAP directory.",
    Kind = ActivityKind.Task)]
[FlowNode(OutcomeNoEntriesFound, OutcomeOneEntryFound, OutcomeMultipleEntriesFound)]
public class SearchLdapEntries : Activity<IEnumerable<SearchResultEntry>>
{
    private const string OutcomeNoEntriesFound = "No entries found";
    private const string OutcomeOneEntryFound = "One entry found";
    private const string OutcomeMultipleEntriesFound = "Multiple entries found";

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
    public Input<SearchScope> Scope { get; set; } = new(SearchScope.Subtree);

    [Input(
        DisplayName = "Attributes",
        Description = "The attributes to return. Leave empty to return all attributes.")]
    public Input<string[]?> Attributes { get; set; } = default!;

    [Output(
        DisplayName = "Search Results",
        Description = "The search results in serializable form.")]
    public Output<IEnumerable<Dictionary<string, string[]>>> SearchResults { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var logger = context.GetRequiredService<ILogger<SearchLdapEntries>>();
        var ldapConnectionFactory = context.GetRequiredService<ILdapConnectionFactory>();

        var connectionName = context.Get(ConnectionName);
        var baseDn = context.Get(BaseDn)!;
        var filter = context.Get(Filter)!;
        var scope = context.Get(Scope);
        var attributes = context.Get(Attributes);

        using var connection = ldapConnectionFactory.CreateConnection(connectionName);

        var result = new List<SearchResultEntry>();
        var request = new SearchRequest(baseDn, filter, scope, attributes);
        
        var response = await connection.SendRequestAsync(request);

        if (response.ResultCode.IsError())
        {
            logger.LogError("{Status} - LDAP request (search entries) failed: {Message}", response.ResultCode, response.ErrorMessage);
        }

        foreach (SearchResultEntry entry in response.Entries)
        {
            result.Add(entry);
        }

        var searchResults = result.Select(x => x.MapToAttributesDictionary()).ToList();

        context.Set(Result, result);
        context.Set(SearchResults, searchResults);

        context.JournalData.Add("ResultCode", response.ResultCode);
        context.JournalData.Add("MatchCount", result.Count);

        var outcome = result.Count switch
        {
            0 => OutcomeNoEntriesFound,
            1 => OutcomeOneEntryFound,
            _ => OutcomeMultipleEntriesFound,
        };

        await context.CompleteActivityWithOutcomesAsync(outcome);
    }
}