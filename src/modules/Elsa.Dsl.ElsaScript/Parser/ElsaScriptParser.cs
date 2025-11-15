using Elsa.Dsl.ElsaScript.Ast;
using Elsa.Dsl.ElsaScript.Contracts;
using System.Text.RegularExpressions;

namespace Elsa.Dsl.ElsaScript.Parser;

/// <summary>
/// A simplified ElsaScript parser that handles basic workflow definitions.
/// This is a minimal implementation to demonstrate the DSL concept.
/// A production version would use Parlot for more robust parsing.
/// </summary>
public class ElsaScriptParser : IElsaScriptParser
{
    /// <inheritdoc />
    public WorkflowNode Parse(string source)
    {
        var workflow = new WorkflowNode();

        // First, tokenize the source into statements
        var statements = TokenizeStatements(source);

        var i = 0;

        // Parse use statements
        while (i < statements.Count && statements[i].StartsWith("use "))
        {
            var useStatement = statements[i];
            if (useStatement.Contains("expressions"))
            {
                var match = Regex.Match(useStatement, @"use\s+expressions\s+(\w+)");
                if (match.Success)
                {
                    workflow.UseStatements.Add(new()
                    {
                        Type = UseType.Expressions,
                        Value = match.Groups[1].Value
                    });
                }
            }
            else
            {
                var match = Regex.Match(useStatement, @"use\s+([\w\.]+)");
                if (match.Success)
                {
                    workflow.UseStatements.Add(new()
                    {
                        Type = UseType.Namespace,
                        Value = match.Groups[1].Value
                    });
                }
            }
            i++;
        }

        // Parse workflow declaration
        if (i < statements.Count && statements[i].StartsWith("workflow "))
        {
            var match = Regex.Match(statements[i], @"workflow\s+""([^""]+)""\s*\{");
            if (match.Success)
            {
                workflow.Name = match.Groups[1].Value;
                i++;
            }
        }

        // Parse body (simple statements only for now)
        while (i < statements.Count && statements[i] != "}")
        {
            var statement = ParseStatement(statements[i]);
            if (statement != null)
            {
                workflow.Body.Add(statement);
            }
            i++;
        }

        return workflow;
    }

    private List<string> TokenizeStatements(string source)
    {
        var statements = new List<string>();
        var current = new System.Text.StringBuilder();
        var inString = false;
        var stringChar = '\0';
        var depth = 0;
        var parenDepth = 0;

        for (int i = 0; i < source.Length; i++)
        {
            var ch = source[i];

            if (!inString)
            {
                if (ch == '"' || ch == '\'')
                {
                    inString = true;
                    stringChar = ch;
                    current.Append(ch);
                }
                else if (ch == '(')
                {
                    parenDepth++;
                    current.Append(ch);
                }
                else if (ch == ')')
                {
                    parenDepth--;
                    current.Append(ch);
                }
                else if (ch == '{')
                {
                    depth++;
                    current.Append(ch);

                    // Opening brace for workflow block is a statement terminator
                    if (depth == 1 && current.ToString().Trim().StartsWith("workflow "))
                    {
                        var trimmed = current.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed))
                        {
                            statements.Add(trimmed);
                        }
                        current.Clear();
                    }
                }
                else if (ch == '}')
                {
                    depth--;

                    // Standalone closing brace at depth 0 is a statement terminator
                    if (depth == 0)
                    {
                        var trimmed = current.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed))
                        {
                            statements.Add(trimmed);
                        }
                        statements.Add("}");
                        current.Clear();
                    }
                    else
                    {
                        current.Append(ch);
                    }
                }
                else if (ch == ';' && parenDepth == 0)
                {
                    // Semicolon terminates a statement (at any depth, but not inside parens)
                    var trimmed = current.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        statements.Add(trimmed);
                    }
                    current.Clear();
                }
                else if (ch == '\n' && depth > 0 && parenDepth == 0)
                {
                    // Newline inside workflow block (depth > 0) also terminates a statement
                    // but only if we're not inside parentheses
                    var trimmed = current.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        statements.Add(trimmed);
                        current.Clear();
                    }
                }
                else if (!char.IsWhiteSpace(ch) || current.Length > 0)
                {
                    current.Append(ch);
                }
            }
            else
            {
                current.Append(ch);
                if (ch == stringChar && (i == 0 || source[i - 1] != '\\'))
                {
                    inString = false;
                }
            }
        }

        // Add any remaining content
        var final = current.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(final))
        {
            statements.Add(final);
        }

        return statements;
    }

    private StatementNode? ParseStatement(string statement)
    {
        // Variable declaration: var name = value
        var varMatch = Regex.Match(statement, @"(var|let|const)\s+(\w+)\s*=\s*(.+)");
        if (varMatch.Success)
        {
            var kind = varMatch.Groups[1].Value switch
            {
                "var" => VariableKind.Var,
                "let" => VariableKind.Let,
                "const" => VariableKind.Const,
                _ => VariableKind.Var
            };

            return new VariableDeclarationNode
            {
                Kind = kind,
                Name = varMatch.Groups[2].Value,
                Value = ParseExpression(varMatch.Groups[3].Value)
            };
        }

        // Activity invocation: ActivityName(args)
        var actMatch = Regex.Match(statement, @"(\w+)\s*\(([^)]*)\)");
        if (actMatch.Success)
        {
            var activityName = actMatch.Groups[1].Value;
            var argsStr = actMatch.Groups[2].Value;

            var activity = new ActivityInvocationNode
            {
                ActivityName = activityName,
                Arguments = ParseArguments(argsStr)
            };

            return activity;
        }

        // Listen statement: listen ActivityName(args)
        var listenMatch = Regex.Match(statement, @"listen\s+(\w+)\s*\(([^)]*)\)");
        if (listenMatch.Success)
        {
            var activityName = listenMatch.Groups[1].Value;
            var argsStr = listenMatch.Groups[2].Value;

            var activity = new ActivityInvocationNode
            {
                ActivityName = activityName,
                Arguments = ParseArguments(argsStr)
            };

            return new ListenNode { Activity = activity };
        }

        return null;
    }

    private List<ArgumentNode> ParseArguments(string argsStr)
    {
        var arguments = new List<ArgumentNode>();
        
        if (string.IsNullOrWhiteSpace(argsStr))
        {
            return arguments;
        }
        
        // Simple parsing - split by comma (doesn't handle nested commas)
        var parts = SplitArguments(argsStr);
        
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            
            // Named argument: Name: value
            var namedMatch = Regex.Match(trimmed, @"(\w+)\s*:\s*(.+)");
            if (namedMatch.Success)
            {
                arguments.Add(new()
                {
                    Name = namedMatch.Groups[1].Value,
                    Value = ParseExpression(namedMatch.Groups[2].Value)
                });
            }
            else
            {
                // Positional argument
                arguments.Add(new()
                {
                    Value = ParseExpression(trimmed)
                });
            }
        }
        
        return arguments;
    }

    private List<string> SplitArguments(string argsStr)
    {
        var parts = new List<string>();
        var current = new System.Text.StringBuilder();
        var depth = 0;
        var inString = false;
        var stringChar = '\0';
        
        foreach (var ch in argsStr)
        {
            if (!inString)
            {
                if (ch == '"' || ch == '\'')
                {
                    inString = true;
                    stringChar = ch;
                    current.Append(ch);
                }
                else if (ch == '(' || ch == '[' || ch == '{')
                {
                    depth++;
                    current.Append(ch);
                }
                else if (ch == ')' || ch == ']' || ch == '}')
                {
                    depth--;
                    current.Append(ch);
                }
                else if (ch == ',' && depth == 0)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(ch);
                }
            }
            else
            {
                current.Append(ch);
                if (ch == stringChar && (current.Length == 1 || current[current.Length - 2] != '\\'))
                {
                    inString = false;
                }
            }
        }
        
        if (current.Length > 0)
        {
            parts.Add(current.ToString());
        }
        
        return parts;
    }

    private ExpressionNode ParseExpression(string expr)
    {
        expr = expr.Trim();
        
        // String literal: "..." or '...'
        if ((expr.StartsWith('"') && expr.EndsWith('"')) || 
            (expr.StartsWith('\'') && expr.EndsWith('\'')))
        {
            var value = expr.Substring(1, expr.Length - 2);
            return new LiteralNode { Value = value };
        }
        
        // Number literal
        if (int.TryParse(expr, out var intValue))
        {
            return new LiteralNode { Value = intValue };
        }
        
        if (decimal.TryParse(expr, out var decValue))
        {
            return new LiteralNode { Value = decValue };
        }
        
        // Boolean literals
        if (expr == "true")
        {
            return new LiteralNode { Value = true };
        }
        
        if (expr == "false")
        {
            return new LiteralNode { Value = false };
        }
        
        // Array literal: [...]
        if (expr.StartsWith('[') && expr.EndsWith(']'))
        {
            var content = expr.Substring(1, expr.Length - 2);
            var elements = SplitArguments(content);
            
            return new ArrayLiteralNode
            {
                Elements = elements.Select(e => ParseExpression(e)).ToList()
            };
        }
        
        // Elsa expression: => "code" or lang => "code"
        var elsaMatch = Regex.Match(expr, @"(?:(\w+)\s+)?=>\s*(.+)");
        if (elsaMatch.Success)
        {
            var language = elsaMatch.Groups[1].Success ? elsaMatch.Groups[1].Value : null;
            var code = elsaMatch.Groups[2].Value.Trim();
            
            // Remove quotes from code if present
            if ((code.StartsWith('"') && code.EndsWith('"')) || 
                (code.StartsWith('\'') && code.EndsWith('\'')))
            {
                code = code.Substring(1, code.Length - 2);
            }
            
            return new ElsaExpressionNode
            {
                Language = language,
                Expression = code
            };
        }
        
        // Identifier (variable reference)
        if (Regex.IsMatch(expr, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
        {
            return new IdentifierNode { Name = expr };
        }
        
        // Default to literal
        return new LiteralNode { Value = expr };
    }
}
