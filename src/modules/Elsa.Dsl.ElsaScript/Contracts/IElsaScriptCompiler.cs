using Elsa.Dsl.ElsaScript.Ast;
using Elsa.Workflows.Activities;

namespace Elsa.Dsl.ElsaScript.Contracts;

/// <summary>
/// Compiles an ElsaScript AST into an Elsa workflow.
/// </summary>
public interface IElsaScriptCompiler
{
    /// <summary>
    /// Compiles the given workflow AST into an Elsa workflow.
    /// </summary>
    /// <param name="workflowNode">The workflow AST.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The compiled Elsa workflow.</returns>
    Task<Workflow> CompileAsync(WorkflowNode workflowNode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses and compiles the given ElsaScript source code into an Elsa workflow.
    /// </summary>
    /// <param name="source">The ElsaScript source code.</param>
    /// /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The compiled Elsa workflow.</returns>
    Task<Workflow> CompileAsync(string source, CancellationToken cancellationToken = default);
}
