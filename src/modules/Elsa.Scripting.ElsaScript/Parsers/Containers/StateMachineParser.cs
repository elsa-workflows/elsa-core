using Parlot.Fluent;
using Elsa.Scripting.ElsaScript.Ast;
using static Parlot.Fluent.Parsers;

namespace Elsa.Scripting.ElsaScript.Parsers.Containers;

public static class StateMachineParser
{
    public static readonly Parser<Statement> Instance = StateMachine();

    private static Parser<Statement> StateMachine()
    {
        // Placeholder: statemachine { ... }
        // Not implemented for this iteration
        return Terms.Text("statemachine")
            .SkipAnd(Between(Terms.Char('{'), ZeroOrMany(AnyCharBefore(Terms.Char('}'))), Terms.Char('}')))
            .Then<Statement>(_ => new StateMachine { Start = "initial" });
    }
}

