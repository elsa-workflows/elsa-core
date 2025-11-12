using System.Collections.Generic;
using System.Linq;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;
using Elsa.Scripting.ElsaScript.Ast;
using static Elsa.Scripting.ElsaScript.Parsing.Helpers;
using static Elsa.Scripting.ElsaScript.Parsing.P;

namespace Elsa.Scripting.ElsaScript.Parsing;

internal static class ControlParsers
{
    public static void AddForWhileSwitch(
        ref Parser<Node> statement,
        Parser<Expr> expr,
        Parser<Node> singleOrBlock,
        Parser<Node> block,
        Parser<Node> varDecl,
        Parser<Node> letDecl,
        Parser<Node> constDecl)
    {
        var lpar = Terms.Char('(');
        var rpar = Terms.Char(')');
        var semi = Terms.Char(';');
        var colon = Terms.Char(':');
        var lbrace = Terms.Char('{');
        var rbrace = Terms.Char('}');

        var forKw = Keyword("for");
        var whileKw = Keyword("while");
        var switchKw = Keyword("switch");
        var caseKw = Keyword("case");
        var defaultKw = Keyword("default");

        var exprStmt = expr.Then<Node>(e => new ExprStmt(e, SourceSpan.Empty));

        var forInit =
            constDecl.Or(varDecl).Or(letDecl).Or(exprStmt)
                .Optional();

        var forHeader =
            Between(lpar,
                forInit.And(semi)
                       .And(expr.Optional())
                       .And(semi)
                       .And(expr.Optional()),
                rpar);

        var forStmt =
            forKw.And(forHeader).And(singleOrBlock)
                .Then(t => (Node)new ForNode(
                    t.Item1.Item2.Item1.HasValue ? t.Item1.Item2.Item1.Value : null,
                    t.Item1.Item2.Item2.HasValue ? t.Item1.Item2.Item2.Value : null,
                    t.Item1.Item2.Item3.HasValue ? t.Item1.Item2.Item3.Value : null,
                    t.Item2,
                    SourceSpan.Empty));

        var whileStmt =
            whileKw.And(Between(lpar, expr, rpar)).And(singleOrBlock)
                .Then(t => (Node)new WhileNode(t.Item1.Item2, t.Item2, SourceSpan.Empty));

        var caseBody = block.Then(b => (BlockNode)b);

        var caseClause =
            caseKw.And(expr).And(colon).And(caseBody)
                .Then(t => (Node)new SwitchCaseNode(t.Item1.Item2, t.Item2.Item2, SourceSpan.Empty));

        var defaultClause =
            defaultKw.And(colon).And(caseBody)
                .Then(t => (Node)new SwitchDefaultNode(t.Item2, SourceSpan.Empty));

        var switchBlock =
            Between(lbrace,
                Many(caseClause).And(defaultClause.Optional()),
                rbrace)
                .Then(t =>
                {
                    var cases = t.Item1.Select(n => (SwitchCaseNode)n).ToList();
                    var def = t.Item2.HasValue ? (SwitchDefaultNode)t.Item2.Value : null;
                    return (cases, def);
                });

        var switchStmt =
            switchKw.And(Between(lpar, expr, rpar)).And(switchBlock)
                .Then(t =>
                {
                    var (cases, def) = t.Item2;
                    return (Node)new SwitchNode(t.Item1.Item2, cases, def, SourceSpan.Empty);
                });

        statement = OneOf<Node>(forStmt, whileStmt, switchStmt, statement);
    }
}
