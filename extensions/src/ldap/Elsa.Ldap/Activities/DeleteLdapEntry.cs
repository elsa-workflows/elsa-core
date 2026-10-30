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
/// Delete entry from an LDAP directory.
/// </summary>
[Activity(
    Namespace = "Elsa",
    Category = "LDAP",
    DisplayName = "Delete LDAP entry",
    Description = "Deletes an entry from an LDAP directory.")]
[FlowNode(OutcomeSuccess, OutcomeFailure)]
public class DeleteLdapEntry : Activity<bool>
{
    private const string OutcomeSuccess = "Success";
    private const string OutcomeFailure = "Failure";

    [Input(
        DisplayName = "Connection Name",
        Description = "The name of the LDAP connection to use, as configured in the module options. Defaults to 'Default'.",
        UIHandler = typeof(LdapConnectionDropdownOptionsProvider),
        UIHint = InputUIHints.DropDown)]
    public Input<string?> ConnectionName { get; set; } = default!;

    [Input(
        DisplayName = "Entry DN",
        Description = "The distinguished name of the entry to delete.")]
    public Input<string> EntryDn { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var logger = context.GetRequiredService<ILogger<DeleteLdapEntry>>();
        var ldapConnectionFactory = context.GetRequiredService<ILdapConnectionFactory>();

        var connectionName = context.Get(ConnectionName);
        var entryDn = context.Get(EntryDn)!;

        using var connection = ldapConnectionFactory.CreateConnection(connectionName);

        var request = new DeleteRequest(entryDn);

        var response = await connection.SendRequestAsync(request);

        if (response.ResultCode.IsError())
        {
            logger.LogError("{Status} - LDAP request (delete entry) failed: {Message}", response.ResultCode, response.ErrorMessage);
        }

        var result = response.ResultCode.IsSuccess();

        context.Set(Result, result);

        context.JournalData.Add("ResultCode", response.ResultCode);
        context.JournalData.Add("DeletedEntry", entryDn);

        await context.CompleteActivityWithOutcomesAsync(result ? OutcomeSuccess : OutcomeFailure);
    }
}