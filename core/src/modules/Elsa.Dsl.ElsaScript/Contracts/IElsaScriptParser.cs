using Elsa.Dsl.ElsaScript.Ast;

namespace Elsa.Dsl.ElsaScript.Contracts;

/// <summary>
/// Parses ElsaScript source code into an AST.
/// </summary>
public interface IElsaScriptParser
{
    /// <summary>
    /// Parses the given source code into a program AST that can contain multiple workflows.
    /// </summary>
    /// <param name="source">The ElsaScript source code.</param>
    /// <returns>The parsed program AST containing all workflows.</returns>
    ProgramNode Parse(string source);
}
