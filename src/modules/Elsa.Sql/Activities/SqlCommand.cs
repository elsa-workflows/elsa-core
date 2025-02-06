using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Sql.Contracts;
using Elsa.Sql.UIHints;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;

namespace Elsa.Sql.Activities;

/// <summary>
/// Execute given SQL command and returns the number of rows affected.
/// </summary>
[Activity("Elsa", "SQL", "Execute given SQL command and returns the number of rows affected.", DisplayName = "SQL Command", Kind = ActivityKind.Task)]
public class SqlCommand : Activity
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public SqlCommand([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base (source, line)
    {
    }

    /// <summary>
    /// Database client to connect with.
    /// </summary>
    [Input(
        Description = "Database client.",
        UIHint = InputUIHints.DropDown,
        UIHandler = typeof(SqlClientsDropDownProvider))]
    public Input<string?> Client { get; set; } = default!;

    /// <summary>
    /// Connection string.
    /// </summary>
    [Input(
        Description = "Connection string.",
        CanContainSecrets = true)]
    public Input<string?> ConnectionString { get; set; } = default!;

    /// <summary>
    /// Command to run against the database.
    /// </summary>
    [Input(
        Description = "Command to run against the database.",
        UIHint = InputUIHints.SqlEditor)]
    public Input<string?> Command { get; set; } = default!;


    /// <summary>
    /// The number of affected rows.
    /// </summary>
    [Output(
        Description = "The number of rows affected.")]
    public Output<int?> Result { get; set; } = default!;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var factory = context.GetRequiredService<ISqlClientFactory>();
        var client = factory.CreateClient(Client.GetOrDefault(context), ConnectionString.GetOrDefault(context));

        var result = await client.ExecuteCommandAsync(Command.GetOrDefault(context));
        context.Set(Result, result);

        await CompleteAsync(context);
    }
}