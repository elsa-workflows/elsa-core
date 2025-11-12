using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Elsa.Scripting.ElsaScript.Ast;
using Parlot;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;
using static Elsa.Scripting.ElsaScript.Parsing.P;

namespace Elsa.Scripting.ElsaScript.Parsing;

public class ElsaScriptParser
{
    private readonly Parser<IList<Node>> _program;

    public ElsaScriptParser()
    {
        _program = BuildProgram();
    }

    public ProgramNode Parse(string text)
    {
        var nodes = _program.Parse(text);
        var defaultLanguage = Languages.JavaScript;
        foreach (var node in nodes)
            if (node is UseExpressionsNode use)
                defaultLanguage = use.Language;

        return new ProgramNode(nodes, new SourceSpan(0, text.Length), defaultLanguage);
    }

    private static Parser<IList<Node>> BuildProgram()
    {
        var statement = Deferred<Node>();

        var lbrace = Terms.Char('{');
        var rbrace = Terms.Char('}');
        var lpar = Terms.Char('(');
        var rpar = Terms.Char(')');
        var comma = Terms.Char(',');
        var colon = Terms.Char(':');
        var semi = Terms.Char(';');
        var assign = Terms.Char('=');
        var arrow = Terms.Text("=>");
        var listenKw = Keyword("listen");
        var asKw = Keyword("as");

        var identifier = P.Identifier.Then(x => x.ToString());
        var typeIdentifier = identifier;

        var stringLiteral = P.StringLiteral.Then(span => new LiteralExpr(span.ToString().Substring(1, span.Length - 2), Helpers.Span(span)) as Expr);
        var numberLiteral = P.Number.Then(span =>
        {
            var text = span.ToString();
            if (text.Contains('.') || text.Contains('e') || text.Contains('E'))
                return (Expr)new LiteralExpr(double.Parse(text, System.Globalization.CultureInfo.InvariantCulture), Helpers.Span(span));
            return new LiteralExpr(long.Parse(text, System.Globalization.CultureInfo.InvariantCulture), Helpers.Span(span));
        });
        var boolLiteral = P.Boolean.Then(span => (Expr)new LiteralExpr(bool.Parse(span.ToString()), Helpers.Span(span)));
        var nullLiteral = P.Null.Then(span => (Expr)new LiteralExpr(null, Helpers.Span(span)));
        var identifierExpr = P.Identifier.Then(span => (Expr)new IdentifierExpr(span.ToString(), Helpers.Span(span)));

        var language = OneOf(
            Terms.Text(Languages.JavaScript),
            Terms.Text(Languages.CSharp),
            Terms.Text(Languages.Python),
            Terms.Text(Languages.Liquid))
            .Then(span => span.ToString());

        var rawExpression = CreateRawExpressionParser();
        var lambdaExpr =
            language.Optional()
                .And(arrow)
                .And(rawExpression)
                .Then(t =>
                {
                    var lang = t.Item1.HasValue ? t.Item1.Value : null;
                    return (Expr)new LambdaExpr(lang, t.Item2.Code, SourceSpan.Empty);
                });

        var expression = Deferred<Expr>();
        expression.Parser = OneOf(lambdaExpr, stringLiteral, numberLiteral, boolLiteral, nullLiteral, identifierExpr, rawExpression.Then(x => (Expr)x));

        var exprList = Separated(comma, expression).Optional()
            .Then(x => x.HasValue ? x.Value : Array.Empty<Expr>());

        var namedArg = identifier.And(colon).And(expression)
            .Then(t => (Name: (string?)t.Item1.Item1, Value: t.Item2));

        var positionalArg = expression.Then(x => (Name: (string?)null, Value: x));

        var arg = namedArg.Or(positionalArg);
        var argList = Separated(comma, arg).Optional().Then(x => x.HasValue ? x.Value : Array.Empty<(string? Name, Expr Value)>());

        var typeArgs = Between(Terms.Char('<'), Separated(comma, typeIdentifier), Terms.Char('>')).Optional()
            .Then(x => x.HasValue ? (IList<string>)x.Value : Array.Empty<string>());

        var activityInvocation =
            identifier.And(typeArgs)
                .And(Between(lpar, argList, rpar))
                .Then(t => new ActivityInvocation(
                    t.Item1.Item1,
                    t.Item1.Item2.ToList(),
                    t.Item2.ToList(),
                    false,
                    null,
                    SourceSpan.Empty));

        var activityStmt =
            listenKw.Optional().And(activityInvocation).And(asKw.And(identifier).Optional())
                .Then(t =>
                {
                    var listen = t.Item1.HasValue;
                    var invocation = t.Item2;
                    if (listen)
                        invocation = invocation with { Listen = true };
                    if (t.Item3.HasValue)
                        invocation = invocation with { Alias = t.Item3.Value.Item2 };
                    return (Node)new ActivityStmt(invocation, SourceSpan.Empty);
                });

        var block = Between(lbrace, Many(statement), rbrace)
            .Then(stmts => (Node)new BlockNode(stmts.ToList(), SourceSpan.Empty));

        var varDecl =
            Keyword("var").And(identifier)
                .And((Terms.Char(':').And(typeIdentifier)).Optional())
                .And((assign.And(expression)).Optional())
                .Then(t => (Node)new VarDeclNode(
                    t.Item1.Item2,
                    t.Item2.HasValue ? t.Item2.Value.Item2 : null,
                    t.Item3.HasValue ? t.Item3.Value.Item2 : null,
                    SourceSpan.Empty));

        var letDecl =
            Keyword("let").And(identifier)
                .And((Terms.Char(':').And(typeIdentifier)).Optional())
                .And((assign.And(expression)).Optional())
                .Then(t => (Node)new LetDeclNode(
                    t.Item1.Item2,
                    t.Item2.HasValue ? t.Item2.Value.Item2 : null,
                    t.Item3.HasValue ? t.Item3.Value.Item2 : null,
                    SourceSpan.Empty));

        var constDecl =
            Keyword("const").And(identifier)
                .And((Terms.Char(':').And(typeIdentifier)).Optional())
                .And(assign.And(expression))
                .Then(t => (Node)new ConstDeclNode(
                    t.Item1.Item2,
                    t.Item2.HasValue ? t.Item2.Value.Item2 : null,
                    t.Item3.Item2,
                    SourceSpan.Empty));

        var ifStmt =
            Keyword("if").And(Between(lpar, expression, rpar)).And(Helpers.SingleOrBlock(statement, block)).And((Keyword("else").And(Helpers.SingleOrBlock(statement, block))).Optional())
                .Then(t =>
                {
                    var thenNode = t.Item2;
                    var elseNode = t.Item3.HasValue ? t.Item3.Value.Item2 : null;
                    return (Node)new IfNode(t.Item1.Item2, thenNode, elseNode, SourceSpan.Empty);
                });

        var foreachHeader = Between(lpar,
                OneOf(Keyword("let"), Keyword("var"), Keyword("const")).Then(span => span.ToString())
                    .And(identifier)
                    .And(Keyword("in"))
                    .And(expression)
                    .Then(t =>
                    {
                        var decl = t.Item1.Item1;
                        var items = t.Item2;
                        return (Kind: decl.Item1, Name: decl.Item2, Items: items);
                    }),
                rpar);

        var foreachStmt =
            Keyword("foreach")
                .And(foreachHeader)
                .And(Helpers.SingleOrBlock(statement, block))
                .Then(t =>
                    (Node)new ForEachNode(t.Item1.Item2.Name, t.Item1.Item2.Kind, t.Item1.Item2.Items, t.Item2, SourceSpan.Empty));

        var useExpressions =
            Keyword("use").And(Keyword("expressions")).And(language)
                .Then(t => (Node)new UseExpressionsNode(t.Item2, SourceSpan.Empty));

        var useStrict =
            Keyword("use").And(Keyword("strict")).And(Keyword("types"))
                .Then(_ => (Node)new UseStrictNode(SourceSpan.Empty));

        var useImport =
            Keyword("use").And(identifier).And(Many(Terms.Char('.').And(identifier)))
                .Then(t =>
                {
                    var name = t.Item2;
                    foreach (var part in t.Item3)
                        name += "." + part.Item2;
                    return (Node)new UseImportNode(name, SourceSpan.Empty);
                });

        var workflowStmt =
            Keyword("workflow").And(P.StringLiteral.Optional())
                .And(block)
                .Then(t =>
                {
                    var name = t.Item1.Item2.HasValue ? t.Item1.Item2.Value.ToString().Trim('\"') : null;
                    return (Node)new WorkflowNode(name, (BlockNode)t.Item2, SourceSpan.Empty);
                });

        var singleOrBlock = Helpers.SingleOrBlock(statement, block);
        ControlParsers.AddForWhileSwitch(ref statement, expression, singleOrBlock, block, varDecl, letDecl, constDecl);

        statement.Parser = OneOf<Node>(
            useExpressions,
            useStrict,
            useImport,
            workflowStmt,
            varDecl,
            letDecl,
            constDecl,
            ifStmt,
            foreachStmt,
            activityStmt,
            block
        )
        .And(semi.Optional())
        .Then(t => t.Item1);

        return Many(statement).Then(stmts => (IList<Node>)stmts.ToList());
    }

    private static Parser<RawExpr> CreateRawExpressionParser()
        => Parsers.Create<RawExpr>((scanner, context) =>
        {
            scanner.SkipWhiteSpace();
            var start = scanner.Cursor.Position;
            var depth = 0;
            var inString = false;
            char stringDelimiter = '\0';

            while (!scanner.Cursor.Eof)
            {
                var ch = scanner.ReadChar();
                if (inString)
                {
                    if (ch == '\\')
                    {
                        scanner.ReadChar();
                        continue;
                    }
                    if (ch == stringDelimiter)
                    {
                        inString = false;
                        continue;
                    }
                }
                else
                {
                    if (ch == '\'' || ch == '\"')
                    {
                        inString = true;
                        stringDelimiter = ch;
                        continue;
                    }

                    if (ch == '(')
                    {
                        depth++;
                        continue;
                    }

                    if (ch == ')')
                    {
                        if (depth == 0)
                        {
                            scanner.Cursor.Position--;
                            break;
                        }
                        depth--;
                        continue;
                    }

                    if (depth == 0 && (ch == ',' || ch == ';'))
                    {
                        scanner.Cursor.Position--;
                        break;
                    }
                }
            }

            var end = scanner.Cursor.Position;
            var text = scanner.Buffer.Substring(start, end - start).Trim();
            if (string.IsNullOrEmpty(text))
                return ParseResult<RawExpr>.Fail(start);

            var span = new TextSpan(scanner.Buffer, start, end - start);
            return ParseResult<RawExpr>.Succeed(new RawExpr(text, Helpers.Span(span)));
        });
}
