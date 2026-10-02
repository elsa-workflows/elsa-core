using System.Data;
using System.Runtime.CompilerServices;
using Elsa.Expressions.Models;
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
    public SqlQuery([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <summary>
    /// Database client to connect with.
    /// </summary>
    [Input(
        Description = "Database client.",
        UIHint = InputUIHints.DropDown,
        UIHandler = typeof(SqlClientsDropDownProvider))]
    public Input<string?> Client { get; set; } = null!;

    /// <summary>
    /// Connection string.
    /// </summary>
    [Input(
        Description = "Connection string.",
        CanContainSecrets = true)]
    public Input<string?> ConnectionString { get; set; } = null!;

    /// <summary>
    /// Query to run against the database.
    /// </summary>
    [Input(
        Description = "Query to run against the database.",
        DefaultSyntax = "Sql",
        UIHint = InputUIHints.CodeEditor,
        UIHandler = typeof(SqlCodeOptionsProvider)
    )]
    public Input<string?> Query { get; set; } = null!;


    /// <summary>
    /// <see cref="DataSet"/> of queried results.
    /// </summary>
    [Output(
        Description = "DataSet of queried results.",
        IsSerializable = false)]
    public Output<DataSet?> Results { get; set; } = null!;


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var query = Query.GetOrDefault(context);

        // If no query was specified, there's nothing to do.
        if (string.IsNullOrWhiteSpace(query))
            return;

        // Get and execute the SQL evaluator.
        var evaluator = context.GetRequiredService<ISqlEvaluator>();
        var evaluatedQuery = await evaluator.EvaluateAsync(query, context.ExpressionExecutionContext, new ExpressionEvaluatorOptions(), context.CancellationToken);

        // Create client
        var factory = context.GetRequiredService<ISqlClientFactory>();
        var client = factory.CreateClient(Client.GetOrDefault(context), ConnectionString.GetOrDefault(context));

        // Execute query
        var results = await client.ExecuteQueryAsync(evaluatedQuery);
        context.Set(Results, results);

        await CompleteAsync(context);
    }
}