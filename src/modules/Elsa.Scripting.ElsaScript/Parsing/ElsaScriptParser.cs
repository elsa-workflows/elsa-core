using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Elsa.Scripting.ElsaScript.Ast;

namespace Elsa.Scripting.ElsaScript.Parsing;

public class ElsaScriptParser
{
    private string _text = string.Empty;
    private int _position;
    private string _defaultExpressionLanguage = Languages.JavaScript;
    private readonly List<Node> _members = new();

    public ProgramNode Parse(string text)
    {
        _text = text;
        _position = 0;
        _members.Clear();
        _defaultExpressionLanguage = Languages.JavaScript;

        while (!IsAtEnd)
        {
            SkipWhitespaceAndComments();
            if (IsAtEnd)
                break;

            var statement = ParseStatement();
            if (statement != null)
                _members.Add(statement);
            SkipWhitespaceAndComments();
            if (Match(';'))
                continue;
        }

        return new ProgramNode(_members, new SourceSpan(0, text.Length), _defaultExpressionLanguage);
    }

    private Node? ParseStatement()
    {
        if (CheckKeyword("use"))
            return ParseUse();
        if (CheckKeyword("workflow"))
            return ParseWorkflow();
        if (CheckKeyword("var"))
            return ParseVarLike("var");
        if (CheckKeyword("let"))
            return ParseVarLike("let");
        if (CheckKeyword("const"))
            return ParseVarLike("const");
        if (CheckKeyword("if"))
            return ParseIf();
        if (CheckKeyword("foreach"))
            return ParseForEach();
        if (CheckKeyword("for"))
            return ParseFor();
        if (CheckKeyword("while"))
            return ParseWhile();
        if (CheckKeyword("switch"))
            return ParseSwitch();
        if (Peek('{'))
            return ParseBlock();

        return ParseActivityStatement();
    }

    private Node ParseUse()
    {
        var start = _position;
        ConsumeKeyword("use");
        SkipWhitespaceAndComments();

        if (CheckKeyword("expressions"))
        {
            ConsumeKeyword("expressions");
            SkipWhitespaceAndComments();
            var language = ParseIdentifier();
            var mapped = MapLanguage(language);
            _defaultExpressionLanguage = mapped;
            return new UseExpressionsNode(mapped, SpanFrom(start));
        }

        if (CheckKeyword("strict"))
        {
            ConsumeKeyword("strict");
            SkipWhitespaceAndComments();
            ConsumeKeyword("types");
            return new UseStrictNode(SpanFrom(start));
        }

        var builder = new StringBuilder();
        builder.Append(ParseIdentifier());
        SkipWhitespaceAndComments();
        while (Match('.'))
        {
            builder.Append('.');
            SkipWhitespaceAndComments();
            builder.Append(ParseIdentifier());
            SkipWhitespaceAndComments();
        }

        return new UseImportNode(builder.ToString(), SpanFrom(start));
    }

    private Node ParseWorkflow()
    {
        var start = _position;
        ConsumeKeyword("workflow");
        SkipWhitespaceAndComments();
        string? name = null;
        if (Peek('"'))
            name = ParseStringLiteral();
        SkipWhitespaceAndComments();
        var body = ParseBlock();
        return new WorkflowNode(name, (BlockNode)body, SpanFrom(start));
    }

    private Node ParseVarLike(string kind)
    {
        var start = _position;
        ConsumeKeyword(kind);
        SkipWhitespaceAndComments();
        var name = ParseIdentifier();
        SkipWhitespaceAndComments();
        string? typeName = null;
        if (Match(':'))
        {
            SkipWhitespaceAndComments();
            typeName = ParseIdentifier();
            SkipWhitespaceAndComments();
        }

        Expr? initializer = null;
        if (Match('='))
        {
            SkipWhitespaceAndComments();
            initializer = ParseExpression(';', ')');
        }

        return kind switch
        {
            "var" => new VarDeclNode(name, typeName, initializer, SpanFrom(start)),
            "let" => new LetDeclNode(name, typeName, initializer, SpanFrom(start)),
            _ => new ConstDeclNode(name, typeName, initializer ?? new LiteralExpr(null, SourceSpan.Empty), SpanFrom(start))
        };
    }

    private Node ParseIf()
    {
        var start = _position;
        ConsumeKeyword("if");
        SkipWhitespaceAndComments();
        Expect('(');
        SkipWhitespaceAndComments();
        var condition = ParseExpression(')');
        Expect(')');
        SkipWhitespaceAndComments();
        var thenNode = ParseStatementOrBlock();
        Node? elseNode = null;
        SkipWhitespaceAndComments();
        if (CheckKeyword("else"))
        {
            ConsumeKeyword("else");
            SkipWhitespaceAndComments();
            elseNode = ParseStatementOrBlock();
        }
        return new IfNode(condition, thenNode, elseNode, SpanFrom(start));
    }

    private Node ParseForEach()
    {
        var start = _position;
        ConsumeKeyword("foreach");
        SkipWhitespaceAndComments();
        Expect('(');
        SkipWhitespaceAndComments();
        string declKind = "let";
        if (CheckKeyword("var"))
            declKind = "var";
        else if (CheckKeyword("const"))
            declKind = "const";
        else if (CheckKeyword("let"))
            declKind = "let";
        ConsumeKeyword(declKind);
        SkipWhitespaceAndComments();
        var name = ParseIdentifier();
        SkipWhitespaceAndComments();
        ConsumeKeyword("in");
        SkipWhitespaceAndComments();
        var items = ParseExpression(')');
        Expect(')');
        SkipWhitespaceAndComments();
        var body = ParseStatementOrBlock();
        return new ForEachNode(name, declKind, items, body, SpanFrom(start));
    }

    private Node ParseFor()
    {
        var start = _position;
        ConsumeKeyword("for");
        SkipWhitespaceAndComments();
        Expect('(');
        SkipWhitespaceAndComments();

        Node? initializer = null;
        if (!Peek(';'))
        {
            if (CheckKeyword("var"))
                initializer = ParseVarLike("var");
            else if (CheckKeyword("let"))
                initializer = ParseVarLike("let");
            else if (CheckKeyword("const"))
                initializer = ParseVarLike("const");
            else
            {
                var expr = ParseExpression(';');
                initializer = new ExprStmt(expr, expr.Span);
            }
        }
        Expect(';');
        SkipWhitespaceAndComments();

        Expr? condition = null;
        if (!Peek(';'))
            condition = ParseExpression(';');
        Expect(';');
        SkipWhitespaceAndComments();

        Expr? iterator = null;
        if (!Peek(')'))
            iterator = ParseExpression(')');
        Expect(')');
        SkipWhitespaceAndComments();

        var body = ParseStatementOrBlock();
        return new ForNode(initializer, condition, iterator, body, SpanFrom(start));
    }

    private Node ParseWhile()
    {
        var start = _position;
        ConsumeKeyword("while");
        SkipWhitespaceAndComments();
        Expect('(');
        SkipWhitespaceAndComments();
        var condition = ParseExpression(')');
        Expect(')');
        SkipWhitespaceAndComments();
        var body = ParseStatementOrBlock();
        return new WhileNode(condition, body, SpanFrom(start));
    }

    private Node ParseSwitch()
    {
        var start = _position;
        ConsumeKeyword("switch");
        SkipWhitespaceAndComments();
        Expect('(');
        SkipWhitespaceAndComments();
        var expression = ParseExpression(')');
        Expect(')');
        SkipWhitespaceAndComments();
        Expect('{');
        SkipWhitespaceAndComments();
        var cases = new List<SwitchCaseNode>();
        SwitchDefaultNode? defaultCase = null;
        while (!Peek('}'))
        {
            if (CheckKeyword("case"))
            {
                var caseStart = _position;
                ConsumeKeyword("case");
                SkipWhitespaceAndComments();
                var value = ParseExpression(':');
                Expect(':');
                SkipWhitespaceAndComments();
                var body = ParseBlock();
                cases.Add(new SwitchCaseNode(value, (BlockNode)body, SpanFrom(caseStart)));
            }
            else if (CheckKeyword("default"))
            {
                var defaultStart = _position;
                ConsumeKeyword("default");
                SkipWhitespaceAndComments();
                Expect(':');
                SkipWhitespaceAndComments();
                var body = ParseBlock();
                defaultCase = new SwitchDefaultNode((BlockNode)body, SpanFrom(defaultStart));
            }
            else
                throw CreateError("Unexpected token inside switch block.");
            SkipWhitespaceAndComments();
        }
        Expect('}');
        return new SwitchNode(expression, cases, defaultCase, SpanFrom(start));
    }

    private Node ParseActivityStatement()
    {
        var start = _position;
        var listen = false;
        if (CheckKeyword("listen"))
        {
            ConsumeKeyword("listen");
            listen = true;
            SkipWhitespaceAndComments();
        }

        var name = ParseIdentifier();
        SkipWhitespaceAndComments();
        var typeArgs = new List<string>();
        if (Match('<'))
        {
            do
            {
                SkipWhitespaceAndComments();
                typeArgs.Add(ParseIdentifier());
                SkipWhitespaceAndComments();
            } while (Match(','));
            Expect('>');
        }

        SkipWhitespaceAndComments();
        Expect('(');
        var args = ParseArgumentList();
        Expect(')');
        SkipWhitespaceAndComments();
        string? alias = null;
        if (CheckKeyword("as"))
        {
            ConsumeKeyword("as");
            SkipWhitespaceAndComments();
            alias = ParseIdentifier();
        }

        var invocation = new ActivityInvocation(name, typeArgs, args, listen, alias, SpanFrom(start));
        return new ActivityStmt(invocation, SpanFrom(start));
    }

    private IList<(string? Name, Expr Value)> ParseArgumentList()
    {
        var args = new List<(string? Name, Expr Value)>();
        SkipWhitespaceAndComments();
        if (Peek(')'))
            return args;

        while (true)
        {
            SkipWhitespaceAndComments();
            string? name = null;
            var checkpoint = _position;
            if (TryParseIdentifier(out var maybeName))
            {
                SkipWhitespaceAndComments();
                if (Match(':'))
                {
                    name = maybeName;
                    SkipWhitespaceAndComments();
                }
                else
                {
                    _position = checkpoint;
                }
            }

            var expr = ParseExpression(',', ')');
            args.Add((name, expr));
            SkipWhitespaceAndComments();
            if (Match(','))
                continue;
            break;
        }

        return args;
    }

    private Node ParseStatementOrBlock()
    {
        SkipWhitespaceAndComments();
        if (Peek('{'))
            return ParseBlock();
        var node = ParseStatement();
        SkipWhitespaceAndComments();
        Match(';');
        return node;
    }

    private BlockNode ParseBlock()
    {
        var start = _position;
        Expect('{');
        var statements = new List<Node>();
        while (true)
        {
            SkipWhitespaceAndComments();
            if (Peek('}'))
                break;
            var statement = ParseStatement();
            if (statement != null)
                statements.Add(statement);
            SkipWhitespaceAndComments();
            Match(';');
        }
        Expect('}');
        return new BlockNode(statements, SpanFrom(start));
    }

    private Expr ParseExpression(params char[] terminators)
    {
        SkipWhitespaceAndComments();
        var start = _position;
        if (Match('"'))
        {
            var value = ReadUntil('"');
            Expect('"');
            return new LiteralExpr(value, SpanFrom(start));
        }

        if (Match('`'))
        {
            var code = ReadUntil('`');
            Expect('`');
            var expr = new TemplateStringExpr(new object[] { code }, SpanFrom(start));
            return expr;
        }

        if (MatchKeyword("true"))
            return new LiteralExpr(true, SpanFrom(start));
        if (MatchKeyword("false"))
            return new LiteralExpr(false, SpanFrom(start));

        if (TryParseLambda(out var lambda, terminators))
            return lambda;

        if (char.IsDigit(CurrentChar))
        {
            var numberText = ReadWhile(c => char.IsDigit(c) || c == '.');
            if (numberText.Contains('.'))
            {
                var dbl = double.Parse(numberText, CultureInfo.InvariantCulture);
                return new LiteralExpr(dbl, SpanFrom(start));
            }
            else
            {
                var integer = int.Parse(numberText, CultureInfo.InvariantCulture);
                return new LiteralExpr(integer, SpanFrom(start));
            }
        }

        if (TryParseIdentifier(out var identifier))
        {
            return new IdentifierExpr(identifier, SpanFrom(start));
        }

        var raw = ReadUntilTerminator(terminators);
        return new RawExpr(raw, SpanFrom(start));
    }

    private bool TryParseLambda(out LambdaExpr expr, params char[] terminators)
    {
        var start = _position;
        if (MatchSequence("=>"))
        {
            SkipWhitespaceAndComments();
            var code = ReadUntilTerminator(terminators);
            expr = new LambdaExpr(_defaultExpressionLanguage, code.Trim(), SpanFrom(start));
            return true;
        }

        var checkpoint = _position;
        if (TryParseIdentifier(out var language))
        {
            SkipWhitespaceAndComments();
            if (MatchSequence("=>"))
            {
                SkipWhitespaceAndComments();
                var code = ReadUntilTerminator(terminators);
                expr = new LambdaExpr(MapLanguage(language), code.Trim(), SpanFrom(start));
                return true;
            }
        }

        _position = checkpoint;
        expr = null!;
        return false;
    }

    private string ReadUntilTerminator(params char[] terminators)
    {
        var start = _position;
        var depth = 0;
        while (!IsAtEnd)
        {
            var c = CurrentChar;
            if (c == '(')
                depth++;
            else if (c == ')')
            {
                if (depth == 0 && Array.IndexOf(terminators, ')') >= 0)
                    break;
                depth--;
            }
            else if (Array.IndexOf(terminators, c) >= 0 && depth == 0)
                break;
            _position++;
        }
        return _text.Substring(start, _position - start).Trim();
    }

    private bool TryParseIdentifier(out string identifier)
    {
        if (!IsIdentifierStart(CurrentChar))
        {
            identifier = string.Empty;
            return false;
        }

        var start = _position;
        _position++;
        while (!IsAtEnd && IsIdentifierPart(CurrentChar))
            _position++;
        identifier = _text.Substring(start, _position - start);
        return true;
    }

    private string ParseIdentifier()
    {
        if (!TryParseIdentifier(out var identifier))
            throw CreateError("Expected identifier.");
        return identifier;
    }

    private string ParseStringLiteral()
    {
        Expect('"');
        var start = _position;
        var value = ReadUntil('"');
        Expect('"');
        return value;
    }

    private string ReadUntil(char terminator)
    {
        var start = _position;
        while (!IsAtEnd && CurrentChar != terminator)
        {
            if (CurrentChar == '\\' && PeekNext() == terminator)
            {
                _position += 2;
                continue;
            }
            _position++;
        }
        return _text.Substring(start, _position - start);
    }

    private string ReadWhile(Func<char, bool> predicate)
    {
        var start = _position;
        while (!IsAtEnd && predicate(CurrentChar))
            _position++;
        return _text.Substring(start, _position - start);
    }

    private void SkipWhitespaceAndComments()
    {
        while (!IsAtEnd)
        {
            if (char.IsWhiteSpace(CurrentChar))
            {
                _position++;
                continue;
            }

            if (MatchSequence("//"))
            {
                while (!IsAtEnd && CurrentChar != '\n')
                    _position++;
                continue;
            }

            if (MatchSequence("/*"))
            {
                while (!IsAtEnd && !(CurrentChar == '*' && PeekNext() == '/'))
                    _position++;
                if (!IsAtEnd)
                    _position += 2;
                continue;
            }

            break;
        }
    }

    private bool CheckKeyword(string keyword)
    {
        SkipWhitespaceAndComments();
        var length = keyword.Length;
        if (_position + length > _text.Length)
            return false;
        if (!string.Equals(_text.Substring(_position, length), keyword, StringComparison.Ordinal))
            return false;
        if (_position + length < _text.Length && IsIdentifierPart(_text[_position + length]))
            return false;
        return true;
    }

    private void ConsumeKeyword(string keyword)
    {
        if (!CheckKeyword(keyword))
            throw CreateError($"Expected '{keyword}'.");
        _position += keyword.Length;
    }

    private bool Match(char c)
    {
        SkipWhitespaceAndComments();
        if (!Peek(c))
            return false;
        _position++;
        return true;
    }

    private bool MatchKeyword(string keyword)
    {
        if (CheckKeyword(keyword))
        {
            _position += keyword.Length;
            return true;
        }
        return false;
    }

    private bool MatchSequence(string sequence)
    {
        SkipWhitespaceAndComments();
        var length = sequence.Length;
        if (_position + length > _text.Length)
            return false;
        if (!string.Equals(_text.Substring(_position, length), sequence, StringComparison.Ordinal))
            return false;
        _position += length;
        return true;
    }

    private void Expect(char c)
    {
        SkipWhitespaceAndComments();
        if (IsAtEnd || CurrentChar != c)
            throw CreateError($"Expected '{c}'.");
        _position++;
    }

    private bool Peek(char c)
    {
        SkipWhitespaceAndComments();
        return !IsAtEnd && CurrentChar == c;
    }

    private char CurrentChar => _position < _text.Length ? _text[_position] : '\0';
    private bool IsAtEnd => _position >= _text.Length;
    private char PeekNext() => _position + 1 < _text.Length ? _text[_position + 1] : '\0';

    private static bool IsIdentifierStart(char c) => char.IsLetter(c) || c == '_';
    private static bool IsIdentifierPart(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '-';

    private SourceSpan SpanFrom(int start) => new(start, Math.Max(0, _position - start));

    private Exception CreateError(string message) => new FormatException(message);

    private static string MapLanguage(string language) => language switch
    {
        "js" => Languages.JavaScript,
        "javascript" => Languages.JavaScript,
        "cs" => Languages.CSharp,
        "csharp" => Languages.CSharp,
        "py" => Languages.Python,
        "python" => Languages.Python,
        "liquid" => Languages.Liquid,
        _ => Languages.JavaScript
    };

    private static class Languages
    {
        public const string JavaScript = "JavaScript";
        public const string CSharp = "CSharp";
        public const string Python = "Python";
        public const string Liquid = "Liquid";
    }
}
