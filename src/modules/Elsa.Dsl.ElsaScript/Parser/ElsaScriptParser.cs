using Elsa.Dsl.ElsaScript.Ast;
using Elsa.Dsl.ElsaScript.Contracts;
using Parlot;
using Parlot.Fluent;

namespace Elsa.Dsl.ElsaScript.Parser;

/// <summary>
/// ElsaScript parser using Parlot for robust parsing.
/// </summary>
public class ElsaScriptParser : IElsaScriptParser
{
    private static readonly Parser<ProgramNode> ProgramParser;

    static ElsaScriptParser()
    {
        // Keywords
        var useKeyword = Terms.Text("use");
        var workflowKeyword = Terms.Text("workflow");
        var expressionsKeyword = Terms.Text("expressions");
        var listenKeyword = Terms.Text("listen");
        var varKeyword = Terms.Text("var");
        var constKeyword = Terms.Text("const");
        var forKeyword = Terms.Text("for");
        var foreachKeyword = Terms.Text("foreach");
        var inKeyword = Terms.Text("in");
        var toKeyword = Terms.Text("to");
        var throughKeyword = Terms.Text("through");
        var stepKeyword = Terms.Text("step");
        var flowchartKeyword = Terms.Text("flowchart");
        var entryKeyword = Terms.Text("entry");

        // Basic tokens
        var identifier = Terms.Identifier();
        var stringLiteral = Terms.String();
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
        var rightArrow = Terms.Text("->");
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

        // Elsa expression: lang => <raw text until matching )>
        // We need to capture raw text after => up to the closing parenthesis
        // This supports nested parentheses by counting depth
        // Use a custom scanner-based parser wrapped in RawExpressionParser
        var rawExpressionText = new RawExpressionParser();

        var elsaExpressionWithLang = identifier
            .And(arrow)
            .And(rawExpressionText)
            .Then<ExpressionNode>(x => new ElsaExpressionNode
            {
                Language = x.Item1.ToString(),
                Expression = x.Item3.ToString().Trim()
            });

        var elsaExpressionWithoutLang = arrow
            .And(rawExpressionText)
            .Then<ExpressionNode>(x => new ElsaExpressionNode
            {
                Language = null,
                Expression = x.Item2.ToString().Trim()
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
                Arguments = []
            });

        var activityInvocation = activityInvocationWithArgs.Or(activityInvocationNoArgs);

        // Variable declaration: var/const name = expr
        var variableKindParser = varKeyword.Or(constKeyword);

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

        // Declare deferred for loop, foreach, and flowchart parsers
        var forStatement = Deferred<StatementNode>();
        var foreachStatement = Deferred<StatementNode>();
        var flowchartStatement = Deferred<StatementNode>();

        statement.Parser = variableDeclaration
            .Or(listenStatement)
            .Or(forStatement)
            .Or(foreachStatement)
            .Or(flowchartStatement)
            .Or(activityStatement);

        // Statement with optional semicolon
        var statementWithSemicolon = statement.And(ZeroOrOne(semicolon)).Then(x => x.Item1);

        // For loop statement: for (var i = 0 to 10 step 1) { body } or for (i = 0 to 10) statement
        // Must be defined after statementWithSemicolon
        var rangeOperator = toKeyword.Or(throughKeyword);

        // For body can be either a block or a single statement
        var forBlockBody = Between(leftBrace, ZeroOrMany(statementWithSemicolon), rightBrace)
            .Then(statements => (StatementNode)(statements.Count == 1
                ? statements.First()
                : new BlockNode { Statements = statements.ToList() }));
        var forSingleStatementBody = statement;
        var forBody = forBlockBody.Or(forSingleStatementBody);

        // For header with optional var: (var i = start to/through end step stepValue)
        // or (i = start to/through end step stepValue)
        // Step clause is optional
        var optionalVarKeyword = ZeroOrOne(varKeyword);
        var optionalStepClause = ZeroOrOne(stepKeyword.And(expression).Then(x => x.Item2));

        var forHeader = Between(leftParen,
            optionalVarKeyword
                .And(identifier)
                .And(equals)
                .And(expression)
                .And(rangeOperator)
                .And(expression)
                .And(optionalStepClause)
                .Then(x => (
                    HasVar: x.Item1 != null,
                    VarName: x.Item2.ToString(),
                    Start: x.Item4,
                    RangeOp: x.Item5.ToString(),
                    End: x.Item6,
                    Step: x.Item7
                )),
            rightParen);

        var forStatementParser = forKeyword
            .And(forHeader)
            .And(forBody)
            .Then<StatementNode>(result =>
            {
                var header = result.Item2;
                var body = result.Item3;

                // Default step to 1 if not specified
                var stepExpr = header.Step ?? new LiteralNode { Value = 1 };

                return new ForNode
                {
                    DeclaresVariable = header.HasVar,
                    VariableName = header.VarName,
                    Start = header.Start,
                    End = header.End,
                    Step = stepExpr,
                    IsInclusive = header.RangeOp == "through",
                    Body = body
                };
            });

        forStatement.Parser = forStatementParser;

        // ForEach statement: foreach (var item in collection) { body } or foreach (item in collection) statement
        // Must be defined after statementWithSemicolon
        // ForEach body can be either a block or a single statement
        var foreachBlockBody = Between(leftBrace, ZeroOrMany(statementWithSemicolon), rightBrace)
            .Then(statements => (StatementNode)(statements.Count == 1
                ? statements.First()
                : new BlockNode { Statements = statements.ToList() }));
        var foreachSingleStatementBody = statement;
        var foreachBody = foreachBlockBody.Or(foreachSingleStatementBody);

        // ForEach header with optional var: (var item in collection) or (item in collection)
        var foreachOptionalVarKeyword = ZeroOrOne(varKeyword);

        var foreachHeader = Between(leftParen,
            foreachOptionalVarKeyword
                .And(identifier)
                .And(inKeyword)
                .And(expression)
                .Then(x => (
                    HasVar: x.Item1 != null,
                    VarName: x.Item2.ToString(),
                    Collection: x.Item4
                )),
            rightParen);

        var foreachStatementParser = foreachKeyword
            .And(foreachHeader)
            .And(foreachBody)
            .Then<StatementNode>(result =>
            {
                var header = result.Item2;
                var body = result.Item3;

                return new ForEachNode
                {
                    DeclaresVariable = header.HasVar,
                    VariableName = header.VarName,
                    Collection = header.Collection,
                    Body = body
                };
            });

        foreachStatement.Parser = foreachStatementParser;

        // Flowchart statement: flowchart { [variables] [nodes] [connections] [entry] }
        // Node declaration: label: statement;
        // Entry declaration: entry label;
        // Connection declaration: source -> target; or source.Outcome -> target;

        // Flowchart body element can be:
        // 1. Variable declaration
        // 2. Node declaration (label: statement)
        // 3. Entry declaration (entry label)
        // 4. Connection declaration (source -> target or source.Outcome -> target)

        // Node declaration: label: activityInvocation; or label: { block }
        // Note: We use activityInvocation directly (not statement) to avoid circular dependency
        // since statement includes flowchart which would include node declarations
        var nodeBlock = Between(leftBrace, ZeroOrMany(statementWithSemicolon), rightBrace)
            .Then<StatementNode>(statements => statements.Count == 1
                ? statements.First()
                : new BlockNode { Statements = statements.ToList() });

        var nodeActivityStatement = activityInvocation.Then<StatementNode>(s => s);

        var nodeDeclaration = identifier
            .And(colon)
            .And(nodeBlock.Or(nodeActivityStatement))
            .And(ZeroOrOne(semicolon))
            .Then(x => new LabeledActivityNode
            {
                Label = x.Item1.ToString(),
                Activity = x.Item3
            });

        // Entry declaration: entry label;
        var entryDeclaration = entryKeyword
            .And(identifier)
            .And(ZeroOrOne(semicolon))
            .Then(x => x.Item2.ToString());

        // Connection declaration: source -> target; or source.Outcome -> target;
        // Source can be: identifier or identifier.identifier (with outcome)
        var optionalOutcome = ZeroOrOne(dot.And(identifier).Then(x => x.Item2.ToString()));

        var connectionSource = identifier
            .And(optionalOutcome)
            .Then(x => (
                SourceLabel: x.Item1.ToString(),
                Outcome: x.Item2
            ));

        var connectionTarget = identifier;

        var connectionDeclaration = connectionSource
            .And(rightArrow)
            .And(connectionTarget)
            .And(ZeroOrOne(semicolon))
            .Then(x => new ConnectionNode
            {
                Source = x.Item1.SourceLabel,
                Outcome = x.Item1.Outcome,
                Target = x.Item3.ToString()
            });

        // Flowchart body element type - try each parser in order
        var flowchartBodyElement = variableDeclaration.Then<object>(v => v)
            .Or(entryDeclaration.Then<object>(e => e))
            .Or(nodeDeclaration.Then<object>(n => n))
            .Or(connectionDeclaration.Then<object>(c => c));

        var flowchartBody = Between(leftBrace, ZeroOrMany(flowchartBodyElement), rightBrace);

        var flowchartStatementParser = flowchartKeyword
            .And(flowchartBody)
            .Then<StatementNode>(result =>
            {
                var bodyElements = result.Item2;

                var variables = new List<VariableDeclarationNode>();
                var nodes = new List<LabeledActivityNode>();
                var connections = new List<ConnectionNode>();
                string? entryPoint = null;

                foreach (var element in bodyElements)
                {
                    if (element is VariableDeclarationNode varDecl)
                        variables.Add(varDecl);
                    else if (element is LabeledActivityNode node)
                        nodes.Add(node);
                    else if (element is ConnectionNode conn)
                        connections.Add(conn);
                    else if (element is string entry)
                        entryPoint = entry;
                }

                return new FlowchartNode
                {
                    Variables = variables,
                    Activities = nodes,
                    Connections = connections,
                    EntryPoint = entryPoint
                };
            });

        flowchartStatement.Parser = flowchartStatementParser;

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

        // Workflow metadata: name: value
        var metadataEntry = identifier
            .And(colon)
            .And(expression)
            .Then(x => (Name: x.Item1.ToString(), Value: EvaluateConstantExpressionStatic(x.Item3)));

        var commaSeparatedMetadata = metadataEntry.And(ZeroOrOne(comma)).Then(x => x.Item1);
        var metadataList = ZeroOrMany(commaSeparatedMetadata);

        // Workflow declaration: workflow Identifier [(metadata)] { [use statements] [statements] }
        var workflowMetadata = Between(leftParen, metadataList, rightParen);

        // Workflow body can contain use statements and regular statements
        var workflowUseStatement = useStatement;

        var workflowBodyElement = Deferred<object>();
        workflowBodyElement.Parser = workflowUseStatement
            .Then<object>(u => u)
            .Or(statementWithSemicolon.Then<object>(s => s));

        var workflowBody = Between(leftBrace, ZeroOrMany(workflowBodyElement), rightBrace);

        var workflowWithMetadata = workflowKeyword
            .And(identifier)
            .And(workflowMetadata)
            .Then(x => (WorkflowId: x.Item2.ToString(), Metadata: x.Item3));

        var workflowWithoutMetadata = workflowKeyword
            .And(identifier)
            .Then(x => (WorkflowId: x.Item2.ToString(), Metadata: (IReadOnlyList<(string Name, object Value)>?)null));

        var workflowHeader = workflowWithMetadata.Or(workflowWithoutMetadata);

        var workflowDeclaration = workflowHeader
            .And(workflowBody)
            .Then(x =>
            {
                var header = x.Item1;
                var bodyElements = x.Item2;

                var metadataDict = new Dictionary<string, object>();
                if (header.Metadata != null)
                {
                    foreach (var entry in header.Metadata)
                    {
                        metadataDict[entry.Name] = entry.Value;
                    }
                }

                // Separate use statements from regular statements in body
                var workflowUses = new List<UseNode>();
                var statements = new List<StatementNode>();

                foreach (var element in bodyElements)
                {
                    if (element is UseNode useNode)
                        workflowUses.Add(useNode);
                    else if (element is StatementNode stmt)
                        statements.Add(stmt);
                }

                return new WorkflowNode
                {
                    Id = header.WorkflowId,
                    Metadata = metadataDict,
                    UseStatements = workflowUses,
                    Body = statements
                };
            });

        // Program with single workflow: [global use statements] [workflow declaration]
        var programWithWorkflow = ZeroOrMany(useStatement)
            .And(workflowDeclaration)
            .Then(x =>
            {
                var globalUses = x.Item1.Select(u => (UseNode)u).ToList();
                var workflow = x.Item2;

                return new ProgramNode
                {
                    GlobalUseStatements = globalUses,
                    Workflows = new List<WorkflowNode> { workflow }
                };
            });

        // Fallback: raw statements without workflow keyword (backward compatibility)
        // Only match if there are actual statements (OneOrMany)
        var programWithStatements = ZeroOrMany(useStatement)
            .And(OneOrMany(statementWithSemicolon))
            .Then(x =>
            {
                var globalUses = x.Item1.Select(u => (UseNode)u).ToList();
                var statements = x.Item2.ToList();

                return new ProgramNode
                {
                    GlobalUseStatements = globalUses,
                    Workflows = new List<WorkflowNode>
                    {
                        new WorkflowNode
                        {
                            Id = "DefaultWorkflow",
                            UseStatements = new List<UseNode>(),
                            Body = statements
                        }
                    }
                };
            });

        var programParser = programWithWorkflow.Or(programWithStatements);

        ProgramParser = programParser;
    }

    /// <inheritdoc />
    public ProgramNode Parse(string source)
    {
        if (!ProgramParser.TryParse(source, out var result, out var error))
        {
            throw new ParseException($"Failed to parse ElsaScript: {error}");
        }
        return result;
    }

    /// <summary>
    /// Static helper to evaluate constant expressions during parsing.
    /// </summary>
    private static object EvaluateConstantExpressionStatic(ExpressionNode exprNode)
    {
        return exprNode switch
        {
            LiteralNode literal => literal.Value ?? string.Empty,
            IdentifierNode identifier => identifier.Name,
            _ => string.Empty
        };
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

/// <summary>
/// Custom parser that captures raw text after => until the matching closing parenthesis.
/// Supports nested parentheses.
/// </summary>
internal sealed class RawExpressionParser : Parser<TextSpan>
{
    public override bool Parse(ParseContext context, ref ParseResult<TextSpan> result)
    {
        context.EnterParser(this);

        var scanner = context.Scanner;
        var start = scanner.Cursor.Offset;
        var depth = 0;

        while (!scanner.Cursor.Eof)
        {
            var ch = scanner.Cursor.Current;

            if (ch == '(')
            {
                depth++;
                scanner.Cursor.Advance();
            }
            else if (ch == ')')
            {
                if (depth == 0)
                {
                    // This is the closing paren for the activity invocation
                    break;
                }
                depth--;
                scanner.Cursor.Advance();
            }
            else
            {
                scanner.Cursor.Advance();
            }
        }

        var length = scanner.Cursor.Offset - start;
        if (length == 0)
        {
            context.ExitParser(this);
            return false;
        }

        var text = new TextSpan(scanner.Buffer, start, length);
        result.Set(start, scanner.Cursor.Offset, text);
        context.ExitParser(this);
        return true;
    }
}
