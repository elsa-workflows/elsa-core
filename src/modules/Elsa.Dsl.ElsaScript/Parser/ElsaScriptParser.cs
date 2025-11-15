using Elsa.Dsl.ElsaScript.Ast;
using Elsa.Dsl.ElsaScript.Contracts;
using Parlot;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace Elsa.Dsl.ElsaScript.Parser;

/// <summary>
/// ElsaScript parser using Parlot for robust parsing.
/// </summary>
public class ElsaScriptParser : IElsaScriptParser
{
    private static readonly Parser<WorkflowNode> _workflowParser;

    static ElsaScriptParser()
    {
        // Keywords
        var useKeyword = Terms.Text("use");
        var workflowKeyword = Terms.Text("workflow");
        var expressionsKeyword = Terms.Text("expressions");
        var listenKeyword = Terms.Text("listen");
        var varKeyword = Terms.Text("var");
        var letKeyword = Terms.Text("let");
        var constKeyword = Terms.Text("const");

        // Basic tokens
        var identifier = Terms.Identifier();
        var stringLiteral = Terms.String(StringLiteralQuotes.SingleOrDouble);
        var integerLiteral = Terms.Integer();
        var decimalLiteral = Terms.Decimal();

        // Punctuation
        var semicolon = Terms.Char(';');
        var comma = Terms.Char(',');
        var colon = Terms.Char(':');
        var leftParen = Terms.Char('(');
        var rightParen = Terms.Char(')');
        var leftBrace = Terms.Char('{');
        var rightBrace = Terms.Char('}');
        var leftBracket = Terms.Char('[');
        var rightBracket = Terms.Char(']');
        var dot = Terms.Char('.');
        var arrow = Terms.Text("=>");
        var equals = Terms.Char('=');

        // Deferred parsers for recursive structures
        var expression = Deferred<ExpressionNode>();
        var statement = Deferred<StatementNode>();

        // Expression parsers
        var booleanLiteral = Terms.Text("true").Or(Terms.Text("false"))
            .Then<ExpressionNode>(x => new LiteralNode { Value = x.ToString() == "true" });

        var numberLiteral = decimalLiteral
            .Then<ExpressionNode>(x => new LiteralNode { Value = x });

        var intLiteral = integerLiteral
            .Then<ExpressionNode>(x => new LiteralNode { Value = (long)x });

        var stringExpr = stringLiteral
            .Then<ExpressionNode>(x => new LiteralNode { Value = x.ToString() });

        var identifierExpr = identifier
            .Then<ExpressionNode>(x => new IdentifierNode { Name = x.ToString() });

        // Array literal: [expr, expr, ...]
        var commaSeparatedExpression = expression.And(ZeroOrOne(comma)).Then(x => x.Item1);
        var arrayLiteral = Between(leftBracket, ZeroOrMany(commaSeparatedExpression), rightBracket)
            .Then<ExpressionNode>(elements => new ArrayLiteralNode { Elements = elements.ToList() });

        // Elsa expression: lang => "code" or => "code"
        var elsaExpressionWithLang = identifier
            .And(arrow)
            .And(stringLiteral)
            .Then<ExpressionNode>(x => new ElsaExpressionNode
            {
                Language = x.Item1.ToString(),
                Expression = x.Item3.ToString()
            });

        var elsaExpressionWithoutLang = arrow
            .And(stringLiteral)
            .Then<ExpressionNode>(x => new ElsaExpressionNode
            {
                Language = null,
                Expression = x.Item2.ToString()
            });

        var elsaExpression = elsaExpressionWithLang.Or(elsaExpressionWithoutLang);

        // Expression priority: try most specific first
        expression.Parser = elsaExpression
            .Or(arrayLiteral)
            .Or(booleanLiteral)
            .Or(numberLiteral)
            .Or(intLiteral)
            .Or(stringExpr)
            .Or(identifierExpr);

        // Argument parser: name: value or just value
        var namedArgument = identifier
            .And(colon)
            .And(expression)
            .Then(x => new ArgumentNode { Name = x.Item1.ToString(), Value = x.Item3 });

        var positionalArgument = expression
            .Then(x => new ArgumentNode { Value = x });

        var argument = namedArgument.Or(positionalArgument);

        var commaSeparatedArgument = argument.And(ZeroOrOne(comma)).Then(x => x.Item1);
        var arguments = ZeroOrMany(commaSeparatedArgument);

        // Activity invocation: ActivityName(args) - with or without arguments
        var activityInvocationWithArgs = identifier
            .And(leftParen)
            .And(arguments)
            .And(rightParen)
            .Then(x => new ActivityInvocationNode
            {
                ActivityName = x.Item1.ToString(),
                Arguments = x.Item3.ToList()
            });

        var activityInvocationNoArgs = identifier
            .And(leftParen)
            .And(rightParen)
            .Then(x => new ActivityInvocationNode
            {
                ActivityName = x.Item1.ToString(),
                Arguments = new List<ArgumentNode>()
            });

        var activityInvocation = activityInvocationWithArgs.Or(activityInvocationNoArgs);

        // Variable declaration: var/let/const name = expr
        var variableKindParser = varKeyword.Or(letKeyword).Or(constKeyword);

        var variableDeclaration = variableKindParser
            .And(identifier)
            .And(equals)
            .And(expression)
            .Then<StatementNode>(x =>
            {
                // x is a flat tuple (kind, identifier, equals, expression)
                var kind = x.Item1.ToString() switch
                {
                    "var" => VariableKind.Var,
                    "let" => VariableKind.Let,
                    "const" => VariableKind.Const,
                    _ => VariableKind.Var
                };

                return new VariableDeclarationNode
                {
                    Kind = kind,
                    Name = x.Item2.ToString(),
                    Value = x.Item4
                };
            });

        // Listen statement: listen ActivityName(args)
        var listenStatement = listenKeyword
            .And(activityInvocation)
            .Then<StatementNode>(x => new ListenNode { Activity = x.Item2 });

        // Statement: variable declaration, listen, or activity invocation
        var activityStatement = activityInvocation
            .Then<StatementNode>(x => x);

        statement.Parser = variableDeclaration
            .Or(listenStatement)
            .Or(activityStatement);

        // Statement with optional semicolon
        var statementWithSemicolon = statement.And(ZeroOrOne(semicolon)).Then(x => x.Item1);

        // Use statement: use Namespace; or use expressions lang;
        var namespaceUse = identifier
            .And(ZeroOrMany(dot.And(identifier)))
            .Then(x =>
            {
                var ns = x.Item1.ToString();
                foreach (var part in x.Item2)
                {
                    ns += "." + part.Item2.ToString();
                }
                return new UseNode { Type = UseType.Namespace, Value = ns };
            });

        var expressionUse = expressionsKeyword
            .And(identifier)
            .Then(x => new UseNode { Type = UseType.Expressions, Value = x.Item2.ToString() });

        var useStatement = useKeyword
            .And(expressionUse.Or(namespaceUse))
            .And(ZeroOrOne(semicolon))
            .Then(x => x.Item2);

        // Workflow block: workflow "name" { statements }
        var workflowDeclaration = workflowKeyword
            .And(stringLiteral)
            .And(Between(leftBrace, ZeroOrMany(statementWithSemicolon), rightBrace))
            .Then(x =>
            {
                // x is (TextSpan, TextSpan, IReadOnlyList<StatementNode>)
                // x.Item1 is workflow keyword
                // x.Item2 is string literal (name)
                // x.Item3 is IReadOnlyList<StatementNode>
                return (x.Item2.ToString(), x.Item3.ToList());
            });

        // Top-level: [use statements] [workflow declaration | statements]
        // Try workflow declaration first, if that fails, try statements
        var workflowOrStatements = workflowDeclaration
            .Or(ZeroOrMany(statementWithSemicolon).Then(stmts => ((string?)null, stmts.ToList())));

        var workflowParser = ZeroOrMany(useStatement)
            .And(workflowOrStatements)
            .Then(x =>
            {
                var useStatements = x.Item1.Select(u => (UseNode)u).ToList();
                var workflowInfo = x.Item2;

                return new WorkflowNode
                {
                    UseStatements = useStatements,
                    Name = workflowInfo.Item1,
                    Body = workflowInfo.Item2 ?? new List<StatementNode>()
                };
            });

        _workflowParser = workflowParser;
    }

    /// <inheritdoc />
    public WorkflowNode Parse(string source)
    {
        if (!_workflowParser.TryParse(source, out var result, out var error))
        {
            throw new ParseException($"Failed to parse ElsaScript: {error}");
        }
        return result;
    }
}

/// <summary>
/// Exception thrown when parsing fails.
/// </summary>
public class ParseException : Exception
{
    public ParseException(string message) : base(message)
    {
    }
}
