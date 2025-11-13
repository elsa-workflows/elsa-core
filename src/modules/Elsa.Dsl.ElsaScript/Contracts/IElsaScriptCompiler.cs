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
    /// <returns>The compiled Elsa workflow.</returns>
    Workflow Compile(WorkflowNode workflowNode);
}
