using System.Collections.Generic;

namespace Elsa.Scripting.ElsaScript.Ast;

public abstract record Node(SourceSpan Span);

public record SourceSpan(int Start, int Length)
{
    public static SourceSpan Empty { get; } = new(0, 0);
}

public abstract record Expr(SourceSpan Span) : Node(Span);

public record LiteralExpr(object? Value, SourceSpan Span) : Expr(Span);

public record IdentifierExpr(string Name, SourceSpan Span) : Expr(Span);

public record TemplateStringExpr(IList<object> Parts, SourceSpan Span) : Expr(Span);

public record LambdaExpr(string Language, string Code, SourceSpan Span) : Expr(Span);

public record RawExpr(string Code, SourceSpan Span) : Expr(Span);

public record ProgramNode(IList<Node> Members, SourceSpan Span, string DefaultExpressionLanguage) : Node(Span);

public abstract record VarBase(string Name, string? TypeName, Expr? Initializer, SourceSpan Span) : Node(Span);

public record VarDeclNode(string Name, string? TypeName, Expr? Initializer, SourceSpan Span) : VarBase(Name, TypeName, Initializer, Span);

public record LetDeclNode(string Name, string? TypeName, Expr? Initializer, SourceSpan Span) : VarBase(Name, TypeName, Initializer, Span);

public record ConstDeclNode(string Name, string? TypeName, Expr Initializer, SourceSpan Span) : VarBase(Name, TypeName, Initializer, Span);

public record ExprStmt(Expr Expression, SourceSpan Span) : Node(Span);

public record BlockNode(IList<Node> Statements, SourceSpan Span) : Node(Span);

public record ActivityInvocation(string Name, IList<string> TypeArguments, IList<(string? Name, Expr Value)> Arguments, bool Listen, string? Alias, SourceSpan Span) : Node(Span);

public record ActivityStmt(ActivityInvocation Invocation, SourceSpan Span) : Node(Span);

public record IfNode(Expr Condition, Node Then, Node? Else, SourceSpan Span) : Node(Span);

public record ForEachNode(string VariableName, string DeclarationKind, Expr Items, Node Body, SourceSpan Span) : Node(Span);

public record ForNode(Node? Initializer, Expr? Condition, Expr? Iterator, Node Body, SourceSpan Span) : Node(Span);

public record WhileNode(Expr Condition, Node Body, SourceSpan Span) : Node(Span);

public record SwitchNode(Expr Expression, IList<SwitchCaseNode> Cases, SwitchDefaultNode? Default, SourceSpan Span) : Node(Span);

public record SwitchCaseNode(Expr Value, BlockNode Body, SourceSpan Span) : Node(Span);

public record SwitchDefaultNode(BlockNode Body, SourceSpan Span) : Node(Span);

public record WorkflowNode(string? Name, BlockNode Body, SourceSpan Span) : Node(Span);

public record UseImportNode(string Namespace, SourceSpan Span) : Node(Span);

public record UseExpressionsNode(string Language, SourceSpan Span) : Node(Span);

public record UseStrictNode(SourceSpan Span) : Node(Span);
