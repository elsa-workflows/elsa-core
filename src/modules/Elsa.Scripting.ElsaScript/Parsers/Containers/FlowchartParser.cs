using Parlot.Fluent;
using Elsa.Scripting.ElsaScript.Ast;
using static Parlot.Fluent.Parsers;

namespace Elsa.Scripting.ElsaScript.Parsers.Containers;

public static class FlowchartParser
{
    public static readonly Parser<Statement> Instance = Flowchart();

    private static Parser<Statement> Flowchart()
    {
        // Placeholder: flowchart { ... }
        // Not implemented for this iteration
        return Terms.Text("flowchart")
            .SkipAnd(Between(Terms.Char('{'), ZeroOrMany(AnyCharBefore(Terms.Char('}'))), Terms.Char('}')))
            .Then<Statement>(_ => new Flowchart());
    }
}

