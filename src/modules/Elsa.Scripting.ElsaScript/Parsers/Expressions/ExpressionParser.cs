using Parlot;
using Parlot.Fluent;
using Elsa.Scripting.ElsaScript.Ast;

namespace Elsa.Scripting.ElsaScript.Parsers.Expressions;

/// <summary>
/// Parsers for expressions.
/// </summary>
public static class ExpressionParser
{
    public static readonly Parser<Expression> Instance = Expression();

    private static Parser<Expression> Expression()
    {
        // Order matters: template string, inline with optional language prefix, identifier, literal
        return TemplateString()
            .Or(InlineExpression())
            .Or(Identifier())
            .Or(Literal());
    }

    private static Parser<InlineExpression> InlineExpression()
    {
        return new CustomInlineExpressionParser();
    }

    private class CustomInlineExpressionParser : Parser<InlineExpression>
    {
        public override bool Parse(ParseContext ctx, ref ParseResult<InlineExpression> result)
        {
            var scanner = ctx.Scanner;
            scanner.SkipWhiteSpace();

            // optional language id before =>
            var mark = scanner.Cursor.Position;
            string? lang = null;
            if (ReadIdentifier(scanner, out var ident))
            {
                scanner.SkipWhiteSpace();
                if (scanner.ReadChar('=') && scanner.ReadChar('>'))
                {
                    lang = ident.ToString();
                }
                else
                {
                    // rewind if no => followed
                    scanner.Cursor.ResetPosition(mark);
                }
            }
            else if (scanner.ReadChar('=') && scanner.ReadChar('>'))
            {
                // default language
                lang = null;
            }
            else
            {
                return false;
            }

            // Read until top-level ',' or ')' (caller will be in arg list context).
            var start = scanner.Cursor.Position;
            var depth = 0;
            bool inSingle = false, inDouble = false, inBacktick = false;
            while (!scanner.Cursor.Eof)
            {
                var peek = scanner.Peek();
                if (peek == null) break;
                var c = peek.Value;

                if (!inSingle && !inDouble && !inBacktick)
                {
                    if (c == '(') { depth++; scanner.ReadChar(); }
                    else if (c == ')') { if (depth == 0) break; depth--; scanner.ReadChar(); }
                    else if (c == ',') { if (depth == 0) break; scanner.ReadChar(); }
                    else if (c == '\'' ) { inSingle = true; scanner.ReadChar(); }
                    else if (c == '"') { inDouble = true; scanner.ReadChar(); }
                    else if (c == '`') { inBacktick = true; scanner.ReadChar(); }
                    else scanner.ReadChar();
                }
                else
                {
                    scanner.ReadChar();
                    if (inSingle && c == '\'' ) inSingle = false;
                    else if (inDouble && c == '"') inDouble = false;
                    else if (inBacktick && c == '`') inBacktick = false;
                    else if (c == '\\') scanner.ReadChar();
                }
            }
            var end = scanner.Cursor.Position;
            var slice = scanner.Buffer[start..end];
            var code = slice.ToString().Trim();
            result.Set(true, true, new InlineExpression { Language = lang, Code = code });
            return true;
        }
    }

    private static Parser<TemplateStringExpression> TemplateString()
    {
        return new CustomTemplateStringParser();
    }

    private class CustomTemplateStringParser : Parser<TemplateStringExpression>
    {
        public override bool Parse(ParseContext ctx, ref ParseResult<TemplateStringExpression> result)
        {
            var scanner = ctx.Scanner;
            scanner.SkipWhiteSpace();
            if (!scanner.ReadChar('`')) return false;

            var parts = new List<TemplatePart>();
            var textBuffer = new System.Text.StringBuilder();

            while (!scanner.Cursor.Eof)
            {
                var ch = scanner.ReadChar();
                if (ch == null) break;
                var c = ch.Value;

                if (c == '`')
                {
                    if (textBuffer.Length > 0)
                    {
                        parts.Add(new TemplatePart { Text = textBuffer.ToString() });
                        textBuffer.Clear();
                    }
                    result.Set(true, true, new TemplateStringExpression { Parts = parts });
                    return true;
                }
                if (c == '$' && scanner.ReadChar('{'))
                {
                    // flush text
                    if (textBuffer.Length > 0)
                    {
                        parts.Add(new TemplatePart { Text = textBuffer.ToString() });
                        textBuffer.Clear();
                    }
                    // parse expression until matching '}'
                    var exprStart = scanner.Cursor.Position;
                    var depth = 1;
                    bool inSingle = false, inDouble = false, inBacktick = false;
                    while (!scanner.Cursor.Eof)
                    {
                        var p = scanner.ReadChar();
                        if (p == null) break;
                        var pc = p.Value;
                        if (!inSingle && !inDouble && !inBacktick)
                        {
                            if (pc == '{') depth++;
                            else if (pc == '}') { depth--; if (depth == 0) break; }
                            else if (pc == '\'' ) inSingle = true;
                            else if (pc == '"') inDouble = true;
                            else if (pc == '`') inBacktick = true;
                        }
                        else
                        {
                            if (inSingle && pc == '\'' ) inSingle = false;
                            else if (inDouble && pc == '"') inDouble = false;
                            else if (inBacktick && pc == '`') inBacktick = false;
                            else if (pc == '\\') scanner.ReadChar();
                        }
                    }
                    var exprEnd = scanner.Cursor.Position - 1; // exclude closing '}'
                    var exprSlice = scanner.Buffer[exprStart .. exprEnd];
                    var exprCode = exprSlice.ToString();
                    parts.Add(new TemplatePart { Expression = new InlineExpression { Code = exprCode } });
                }
                else
                {
                    textBuffer.Append(c);
                }
            }

            return false;
        }
    }

    private static Parser<IdentifierExpression> Identifier()
    {
        return Terms.Identifier().Then(name => new IdentifierExpression { Name = name.ToString() });
    }

    private static Parser<LiteralExpression> Literal()
    {
        return OneOf(
            Terms.Number().Then(n => new LiteralExpression { Value = n }),
            Terms.String(StringLiteralQuotes.SingleOrDouble).Then(s => new LiteralExpression { Value = s.ToString() }),
            Terms.Text("true").Then(_ => new LiteralExpression { Value = true }),
            Terms.Text("false").Then(_ => new LiteralExpression { Value = false })
        );
    }

    private static bool ReadIdentifier(Scanner scanner, out ReadOnlySpan<char> value)
    {
        value = default;
        var start = scanner.Cursor.Position;
        var slice = scanner.ReadIdentifier();
        if (!slice.HasValue)
        {
            scanner.Cursor.ResetPosition(start);
            return false;
        }
        value = slice.Value.Span;
        return true;
    }
}
