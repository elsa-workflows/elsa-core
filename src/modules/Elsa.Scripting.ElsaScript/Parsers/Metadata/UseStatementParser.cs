using Parlot.Fluent;
using Elsa.Scripting.ElsaScript.Ast;
using static Parlot.Fluent.Parsers;

namespace Elsa.Scripting.ElsaScript.Parsers.Metadata;

public static class UseStatementParser
{
    public static readonly Parser<Statement> Instance = Use();

    private static Parser<Statement> Use()
    {
        // use namespace; or use expressions lang; or use strict types;
        // For now, just consume and ignore these directives
        return Terms.Text("use")
            .SkipAnd(OneOrMany(Terms.Identifier().Or(Terms.Text("strict").Or(Terms.Text("expressions")))))
            .AndSkip(ZeroOrOne(Terms.Char(';')))
            .Then<Statement>(_ => new UseStatement { Namespace = "ignored", Directive = "ignored" });
    }
}

