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
/// Execute given SQL command and return a single result.
/// </summary>
[Activity("Elsa", "SQL", "Execute given SQL command and return a single result.", DisplayName = "SQL Single Value", Kind = ActivityKind.Task)]
public class SqlSingleValue : Activity
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public SqlSingleValue([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
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
        DefaultSyntax = "Sql",
        UIHint = InputUIHints.CodeEditor,
        UIHandler = typeof(SqlCodeOptionsProvider)
    )]
    public Input<string?> Query { get; set; } = default!;


    /// <summary>
    /// Command result.
    /// </summary>
    [Output(
        Description = "Command result.")]
    public Output<object?> Result { get; set; } = default!;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var factory = context.GetRequiredService<ISqlClientFactory>();
        var client = factory.CreateClient(Client.GetOrDefault(context), ConnectionString.GetOrDefault(context));

        var result = await client.ExecuteScalarAsync(Query.GetOrDefault(context));
        context.Set(Result, result);

        await CompleteAsync(context);
    }
}