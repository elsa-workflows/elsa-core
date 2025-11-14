using Parlot;
using Parlot.Fluent;
using Elsa.Scripting.ElsaScript.Ast;
using Elsa.Scripting.ElsaScript.Parsers.Expressions;
using Elsa.Scripting.ElsaScript.Parsers.ControlFlow;
using Elsa.Scripting.ElsaScript.Parsers.Containers;
using Elsa.Scripting.ElsaScript.Parsers.Metadata;
using static Parlot.Fluent.Parsers;

namespace Elsa.Scripting.ElsaScript.Parsers;

public static class StatementParser
{
    public static readonly Parser<Statement> Instance = Deferred<Statement>(() => Statement());

    private static Parser<Statement> Statement()
    {
        return OneOf(
            WorkflowStatementParser.Instance,
            VarDeclaration(),
            LetDeclaration(),
            ConstDeclaration(),
            IfStatementParser.Instance,
            ForEachStatementParser.Instance,
            ForStatementParser.Instance,
            WhileStatementParser.Instance,
            SwitchStatementParser.Instance,
            FlowchartParser.Instance,
            StateMachineParser.Instance,
            UseStatementParser.Instance,
            ActivityStatementParser.Instance
        );
    }

    // Keep these public for reuse in other parsers
    public static Parser<Statement> VarDeclaration()
    {
        return Terms.Text("var")
            .SkipAnd(Terms.Identifier())
            .And(ZeroOrOne(Terms.Char(':').SkipAnd(Terms.Identifier())))
            .And(ZeroOrOne(Terms.Char('=').SkipAnd(ExpressionParser.Instance)))
            .AndSkip(ZeroOrOne(Terms.Char(';')))
            .Then<Statement>(result => new VarDeclaration
            {
                Name = result.Item1.Item1.ToString(),
                Type = result.Item1.Item2?.ToString(),
                Initializer = result.Item2
            });
    }

    public static Parser<Statement> LetDeclaration()
    {
        return Terms.Text("let")
            .SkipAnd(Terms.Identifier())
            .And(ZeroOrOne(Terms.Char(':').SkipAnd(Terms.Identifier())))
            .And(ZeroOrOne(Terms.Char('=').SkipAnd(ExpressionParser.Instance)))
            .AndSkip(ZeroOrOne(Terms.Char(';')))
            .Then<Statement>(result => new LetDeclaration
            {
                Name = result.Item1.Item1.ToString(),
                Type = result.Item1.Item2?.ToString(),
                Initializer = result.Item2
            });
    }

    public static Parser<Statement> ConstDeclaration()
    {
        return Terms.Text("const")
            .SkipAnd(Terms.Identifier())
            .And(ZeroOrOne(Terms.Char(':').SkipAnd(Terms.Identifier())))
            .AndSkip(Terms.Char('='))
            .And(ExpressionParser.Instance)
            .AndSkip(ZeroOrOne(Terms.Char(';')))
            .Then<Statement>(result => new ConstDeclaration
            {
                Name = result.Item1.Item1.ToString(),
                Type = result.Item1.Item2?.ToString(),
                Initializer = result.Item2
            });
    }
}
