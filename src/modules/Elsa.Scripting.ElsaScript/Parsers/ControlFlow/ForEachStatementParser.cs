using Parlot;
using Parlot.Fluent;
using Elsa.Scripting.ElsaScript.Ast;
using Elsa.Scripting.ElsaScript.Parsers.Containers;
using static Parlot.Fluent.Parsers;

namespace Elsa.Scripting.ElsaScript.Parsers.ControlFlow;

public static class ForEachStatementParser
{
    public static readonly Parser<Statement> Instance = ForEach();

    private static Parser<Statement> ForEach()
    {
        var header = Terms.Char('(')
            .SkipAnd(ZeroOrOne(Terms.Text("var").Or(Terms.Text("let"))))
            .And(Terms.Identifier())
            .AndSkip(Terms.Text("in"))
            .And(Expressions.ExpressionParser.Instance)
            .AndSkip(Terms.Char(')'));

        return Terms.Text("foreach")
            .SkipAnd(header)
            .And(BlockParser.Instance)
            .Then<Statement>(t => new ForEachStatement
            {
                VariableName = t.Item1.Item1.Item1,
                Items = t.Item1.Item2,
                Body = t.Item2
            });
    }
}
