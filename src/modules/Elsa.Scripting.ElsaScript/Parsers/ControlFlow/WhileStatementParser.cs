using Parlot.Fluent;
using Elsa.Scripting.ElsaScript.Ast;
using Elsa.Scripting.ElsaScript.Parsers.Containers;
using Elsa.Scripting.ElsaScript.Parsers.Expressions;
using static Parlot.Fluent.Parsers;

namespace Elsa.Scripting.ElsaScript.Parsers.ControlFlow;

public static class WhileStatementParser
{
    public static readonly Parser<Statement> Instance = While();

    private static Parser<Statement> While()
    {
        return Terms.Text("while")
            .SkipAnd(Between(Terms.Char('('), ExpressionParser.Instance, Terms.Char(')')))
            .And(BlockParser.Instance)
            .Then<Statement>(t => new WhileStatement { Condition = t.Item1, Body = t.Item2 });
    }
}

