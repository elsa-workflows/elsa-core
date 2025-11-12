using System.Collections.Generic;
using Parlot;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;
using Elsa.Scripting.ElsaScript.Ast;

namespace Elsa.Scripting.ElsaScript.Parsing;

internal static class Helpers
{
    public static Parser<Node> SingleOrBlock(Parser<Node> statement, Parser<Node> block)
        => statement.Or(block);

    public static SourceSpan Span(TextSpan span)
        => new(span.Offset, span.Length);

    public static SourceSpan Span(TextSpan start, TextSpan end)
    {
        var offset = start.Offset;
        var length = end.Offset + end.Length - offset;
        return new SourceSpan(offset, length);
    }

    public static SourceSpan Span(TextSpan start, IList<TextSpan> parts)
    {
        if (parts.Count == 0)
            return Span(start);
        var last = parts[^1];
        return Span(start, last);
    }
}
