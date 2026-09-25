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
/// Execute given SQL command and returns the number of rows affected.
/// </summary>
[Activity("Elsa", "SQL", "Execute given SQL command and returns the number of rows affected.", DisplayName = "SQL Command", Kind = ActivityKind.Task)]
public class SqlCommand : Activity
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public SqlCommand([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
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
    /// Command to run against the database.
    /// </summary>
    [Input(
        Description = "Command to run against the database.",
        DefaultSyntax = "Sql",
        UIHint = InputUIHints.CodeEditor,
        UIHandler = typeof(SqlCodeOptionsProvider)
    )]
    public Input<string?> Command { get; set; } = null!;


    /// <summary>
    /// The number of affected rows.
    /// </summary>
    [Output(
        Description = "The number of rows affected.")]
    public Output<int?> Result { get; set; } = null!;


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var command = Command.GetOrDefault(context);

        // If no command was specified, there's nothing to do.
        if (string.IsNullOrWhiteSpace(command))
            return;

        // Get and execute the SQL evaluator.
        var evaluator = context.GetRequiredService<ISqlEvaluator>();
        var evaluatedQuery = await evaluator.EvaluateAsync(command, context.ExpressionExecutionContext, new ExpressionEvaluatorOptions(), context.CancellationToken);

        // Create client
        var factory = context.GetRequiredService<ISqlClientFactory>();
        var client = factory.CreateClient(Client.GetOrDefault(context), ConnectionString.GetOrDefault(context));

        // Execute command
        var result = await client.ExecuteCommandAsync(evaluatedQuery);
        context.Set(Result, result);

        await CompleteAsync(context);
    }
}