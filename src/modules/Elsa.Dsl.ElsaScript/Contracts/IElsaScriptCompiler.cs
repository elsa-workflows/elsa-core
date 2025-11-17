using Elsa.Dsl.ElsaScript.Ast;
using Elsa.Workflows.Activities;

namespace Elsa.Dsl.ElsaScript.Contracts;

/// <summary>
/// Compiles an ElsaScript AST into Elsa workflows.
/// </summary>
public interface IElsaScriptCompiler
{
    /// <summary>
    /// Parses and compiles the given ElsaScript source code into a workflow.
    /// </summary>
    /// <param name="source">The ElsaScript source code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The compiled workflow.</returns>
    Task<Workflow> CompileAsync(string source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compiles the given program AST into a workflow.
    /// </summary>
    /// <param name="programNode">The program AST.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The compiled workflow.</returns>
    Task<Workflow> CompileAsync(ProgramNode programNode, CancellationToken cancellationToken = default);
}
