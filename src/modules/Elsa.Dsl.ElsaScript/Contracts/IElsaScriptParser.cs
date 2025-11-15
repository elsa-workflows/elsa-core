using Elsa.Dsl.ElsaScript.Ast;

namespace Elsa.Dsl.ElsaScript.Contracts;

/// <summary>
/// Parses ElsaScript source code into an AST.
/// </summary>
public interface IElsaScriptParser
{
    /// <summary>
    /// Parses the given source code into a workflow AST.
    /// </summary>
    /// <param name="source">The ElsaScript source code.</param>
    /// <returns>The parsed workflow AST.</returns>
    WorkflowNode Parse(string source);
}
