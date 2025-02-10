using System.Data;
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
/// Execute given SQL query and return the resulting data.
/// </summary>
[Activity("Elsa", "SQL", "Execute given SQL query and return the resulting data.", DisplayName = "SQL Query", Kind = ActivityKind.Task)]
public class SqlQuery : Activity
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public SqlQuery([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
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
    /// Query to run against the database.
    /// </summary>
    [Input(
        Description = "Query to run against the database.",
        UIHint = InputUIHints.CodeEditor,
        UIHandler = typeof(SqlCodeOptionsProvider)
    )]
    public Input<string?> Query { get; set; } = default!;


    /// <summary>
    /// <see cref="DataSet"/> of queried results.
    /// </summary>
    [Output(
        Description = "DataSet of queried results.",
        IsSerializable = false)]
    public Output<DataSet?> Results { get; set; } = default!;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var factory = context.GetRequiredService<ISqlClientFactory>();
        var client = factory.CreateClient(Client.GetOrDefault(context), ConnectionString.GetOrDefault(context));

        var results = await client.ExecuteQueryAsync(Query.GetOrDefault(context));
        context.Set(Results, results);

        await CompleteAsync(context);
    }
}