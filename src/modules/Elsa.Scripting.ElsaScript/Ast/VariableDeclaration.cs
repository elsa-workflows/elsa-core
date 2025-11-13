namespace Elsa.Scripting.ElsaScript.Ast;

/// <summary>
/// Base class for variable declarations.
/// </summary>
public abstract class VariableDeclaration : Statement
{
    public string Name { get; set; } = null!;
    public string? Type { get; set; } // optional type hint
    public Expression? Initializer { get; set; }
}

/// <summary>
/// Workflow-level variable declaration.
/// </summary>
public class VarDeclaration : VariableDeclaration
{
}

/// <summary>
/// Sequence-level variable declaration.
/// </summary>
public class LetDeclaration : VariableDeclaration
{
}

/// <summary>
/// Read-only sequence-level variable declaration.
/// </summary>
public class ConstDeclaration : VariableDeclaration
{
}
