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
/// Moves an existing entry in an LDAP directory by modifying its distinguished name.
/// </summary>
[Activity(
    Namespace = "Elsa",
    Category = "LDAP",
    DisplayName = "Move LDAP entry",
    Description = "Moves an existing entry in an LDAP directory by modifying its distinguished name.")]
[FlowNode(OutcomeSuccess, OutcomeFailure)]
public class MoveLdapEntry : Activity<bool>
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
        Description = "The distinguished name of the entry to move.")]
    public Input<string> EntryDn { get; set; } = default!;

    [Input(
        DisplayName = "New Parent DN",
        Description = "The distinguished name of the new parent. Leave empty to rename inside current parent.")]
    public Input<string?> NewParentDn { get; set; } = default!;

    [Input(
        DisplayName = "New CN",
        Description = "The new common name of the entry.")]
    public Input<string> NewCn { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var logger = context.GetRequiredService<ILogger<MoveLdapEntry>>();
        var ldapConnectionFactory = context.GetRequiredService<ILdapConnectionFactory>();

        var connectionName = context.Get(ConnectionName);
        var entryDn = context.Get(EntryDn)!;
        var newParentDn = context.Get(NewParentDn);
        var newCn = context.Get(NewCn);

        using var connection = ldapConnectionFactory.CreateConnection(connectionName);

        var request = new ModifyDNRequest(
            distinguishedName: entryDn,
            newParentDistinguishedName: newParentDn,
            newName: newCn);

        var response = await connection.SendRequestAsync(request);

        if (response.ResultCode.IsError())
        {
            logger.LogError("{Status} - LDAP request (move entry) failed: {Message}", response.ResultCode, response.ErrorMessage);
        }

        var result = response.ResultCode.IsSuccess();

        context.Set(Result, result);

        context.JournalData.Add("ResultCode", response.ResultCode);
        context.JournalData.Add("ModifiedEntry", entryDn);

        await context.CompleteActivityWithOutcomesAsync(result ? OutcomeSuccess : OutcomeFailure);
    }
}