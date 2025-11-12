using System;
using Parlot;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace Elsa.Scripting.ElsaScript.Parsing;

internal static class P
{
    public static Parser<TextSpan> Identifier { get; } = Terms.Identifier();
    public static Parser<TextSpan> StringLiteral { get; } = Terms.String(StringLiteralQuotes.Double);
    public static Parser<TextSpan> Number { get; } = Terms.Decimal();
    public static Parser<TextSpan> Boolean { get; } = Terms.Text("true").Or(Terms.Text("false"));
    public static Parser<TextSpan> Null { get; } = Terms.Text("null");
    public static Parser<TextSpan> Keyword(string keyword)
        => Terms.Identifier().Where(span => string.Equals(span.ToString(), keyword, StringComparison.Ordinal));
}
