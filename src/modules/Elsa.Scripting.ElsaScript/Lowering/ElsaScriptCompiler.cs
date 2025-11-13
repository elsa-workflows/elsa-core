using System.Text;
using System.Text.RegularExpressions;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Scripting.ElsaScript.Lowering;

/// <summary>
/// Compiles ElsaScript syntax to Elsa Workflow objects containing real IActivity instances.
/// This is a heuristic implementation using regex and manual parsing.
/// </summary>
public class ElsaScriptCompiler
{
    public Workflow Compile(string script)
    {
        var seq = new Sequence();
        var s = Normalize(script);

        // Remove leading 'use expressions js;' if present.
        s = Regex.Replace(s, @"\buse\s+expressions\s+js\s*;", string.Empty, RegexOptions.IgnoreCase);

        if (TryParseFor(s, out var forAct))
            seq.Activities.Add(forAct!);
        else if (TryParseWhile(s, out var whileAct))
            seq.Activities.Add(whileAct!);
        else if (TryParseSwitch(s, out var switchAct))
            seq.Activities.Add(switchAct!);
        else
            ParseBlockActivities(s, seq);

        // Wrap in Workflow
        return new Workflow(seq);
    }

    private static string Normalize(string s)
    {
        return s.Replace("\r\n", "\n").Trim();
    }

    private static bool TryParseFor(string s, out IActivity? result)
    {
        result = null;
        var idx = s.IndexOf("for", StringComparison.Ordinal);
        if (idx < 0) return false;

        var headerStart = s.IndexOf('(', idx);
        if (headerStart < 0) return false;
        var headerEnd = FindMatchingParen(s, headerStart);
        if (headerEnd < 0) return false;
        var header = s.Substring(headerStart + 1, headerEnd - headerStart - 1);
        var parts = SplitTopLevel(header, ';');
        if (parts.Count != 3) return false;

        var init = parts[0].Trim();
        var cond = parts[1].Trim();
        var iter = parts[2].Trim();

        var bodyStart = s.IndexOf('{', headerEnd);
        var bodyEnd = FindMatchingBrace(s, bodyStart);
        var bodyText = bodyStart >= 0 && bodyEnd > bodyStart ? s.Substring(bodyStart + 1, bodyEnd - bodyStart - 1) : string.Empty;

        var body = new Sequence();
        ParseBlockActivities(bodyText, body);

        // Note: Elsa's For activity expects Start/End/Step, not JavaScript-style initializer/condition/iterator
        // This is a simplified mapping - we use While as a workaround since it's closer to JavaScript semantics
        var whileActivity = new While(
            new Input<bool>(new Expression("JavaScript", TrimArrow(cond))),
            body
        );

        // Store metadata about the original for loop structure for debugging/introspection
        whileActivity.CustomProperties["ElsaScript.Type"] = "for";
        whileActivity.CustomProperties["ElsaScript.Initializer"] = init;
        whileActivity.CustomProperties["ElsaScript.Iterator"] = iter;

        result = whileActivity;
        return true;
    }

    private static bool TryParseWhile(string s, out IActivity? result)
    {
        result = null;
        var idx = s.IndexOf("while", StringComparison.Ordinal);
        if (idx < 0) return false;
        var headerStart = s.IndexOf('(', idx);
        if (headerStart < 0) return false;
        var headerEnd = FindMatchingParen(s, headerStart);
        if (headerEnd < 0) return false;
        var cond = s.Substring(headerStart + 1, headerEnd - headerStart - 1);
        var bodyStart = s.IndexOf('{', headerEnd);
        var bodyEnd = FindMatchingBrace(s, bodyStart);
        var bodyText = bodyStart >= 0 && bodyEnd > bodyStart ? s.Substring(bodyStart + 1, bodyEnd - bodyStart - 1) : string.Empty;
        var body = new Sequence();
        ParseBlockActivities(bodyText, body);

        result = new While(
            new Input<bool>(new Expression("JavaScript", TrimArrow(cond.Trim()))),
            body
        );
        return true;
    }

    private static bool TryParseSwitch(string s, out IActivity? result)
    {
        result = null;
        var idx = s.IndexOf("switch", StringComparison.Ordinal);
        if (idx < 0) return false;
        var headerStart = s.IndexOf('(', idx);
        var headerEnd = FindMatchingParen(s, headerStart);
        if (headerEnd < 0) return false;
        var expr = s.Substring(headerStart + 1, headerEnd - headerStart - 1);
        var bodyStart = s.IndexOf('{', headerEnd);
        var bodyEnd = FindMatchingBrace(s, bodyStart);
        if (bodyStart < 0 || bodyEnd < 0) return false;
        var bodyText = s.Substring(bodyStart + 1, bodyEnd - bodyStart - 1);

        var cases = new List<SwitchCase>();
        Sequence? @default = null;

        // Parse cases: case "X": { ... }
        var caseRegex = new Regex(@"case\s+([^:]+):\s*{", RegexOptions.IgnoreCase);
        var defaultRegex = new Regex(@"default\s*:\s*{", RegexOptions.IgnoreCase);
        var pos = 0;
        while (pos < bodyText.Length)
        {
            var cm = caseRegex.Match(bodyText, pos);
            var dm = defaultRegex.Match(bodyText, pos);
            if (!cm.Success && !dm.Success) break;

            if (cm.Success && (!dm.Success || cm.Index < dm.Index))
            {
                var valText = cm.Groups[1].Value.Trim();
                var blockStart = bodyText.IndexOf('{', cm.Index);
                var blockEnd = FindMatchingBrace(bodyText, blockStart);
                var blockInner = bodyText.Substring(blockStart + 1, blockEnd - blockStart - 1);
                var seq = new Sequence();
                ParseBlockActivities(blockInner, seq);

                // Create a condition expression for this case
                var caseValue = ParseCaseValueAsString(valText);
                var conditionExpr = new Expression("JavaScript", $"({TrimArrow(expr.Trim())}) === {caseValue}");
                cases.Add(new SwitchCase($"case_{caseValue}", conditionExpr, seq));
                pos = blockEnd + 1;
            }
            else
            {
                var blockStart = bodyText.IndexOf('{', dm.Index);
                var blockEnd = FindMatchingBrace(bodyText, blockStart);
                var blockInner = bodyText.Substring(blockStart + 1, blockEnd - blockStart - 1);
                @default = new Sequence();
                ParseBlockActivities(blockInner, @default);
                pos = blockEnd + 1;
            }
        }

        result = new Switch
        {
            Cases = cases,
            Default = @default
        };
        return true;
    }

    private static string ParseCaseValueAsString(string text)
    {
        text = text.Trim();
        // If already quoted, return as-is
        if (text.StartsWith("\"") && text.EndsWith("\""))
            return text;
        // Otherwise, quote it
        return $"\"{text}\"";
    }

    private static void ParseBlockActivities(string body, Sequence seq)
    {
        // Very simple: split by ';' but keep inside quotes/braces/parentheses
        var stmts = SplitTopLevel(body, ';');
        foreach (var st in stmts.Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)))
        {
            var act = ParseActivity(st);
            if (act != null) seq.Activities.Add(act);
        }
    }

    private static IActivity? ParseActivity(string line)
    {
        // listen prefix
        var isListen = false;
        var s = line;
        if (s.StartsWith("listen ", StringComparison.OrdinalIgnoreCase))
        {
            isListen = true;
            s = s.Substring("listen ".Length).Trim();
        }

        // Name(args)
        var m = Regex.Match(s, @"^(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\((?<args>.*)\)$");
        if (!m.Success) return null;
        var name = m.Groups["name"].Value;

        var argsText = m.Groups["args"].Value.Trim();

        // Create the appropriate activity based on name
        IActivity activity;
        switch (name)
        {
            case "WriteLine":
                activity = CreateWriteLine(argsText);
                break;
            case "Log":
                activity = CreateLog(argsText);
                break;
            default:
                // For unknown activities, create a generic Activity with metadata
                activity = CreateGenericActivity(name, argsText);
                break;
        }

        if (isListen)
            activity.SetCanStartWorkflow(true);

        return activity;
    }

    private static IActivity CreateWriteLine(string argsText)
    {
        var inputs = ParseActivityInputs(argsText);

        // WriteLine expects a "Text" input
        if (inputs.TryGetValue("Text", out var textVal))
        {
            return new WriteLine(new Input<string>(ParseExpressionFromValue(textVal)));
        }
        else if (inputs.Count > 0)
        {
            // Use the first positional argument
            var firstVal = inputs.Values.First();
            return new WriteLine(new Input<string>(ParseExpressionFromValue(firstVal)));
        }

        return new WriteLine("");
    }

    private static IActivity CreateLog(string argsText)
    {
        // Log doesn't exist in Elsa core, create a generic activity using DynamicActivity
        var activity = new DynamicActivity
        {
            Type = "Log",
            ExecuteHandler = _ => ValueTask.CompletedTask
        };
        var inputs = ParseActivityInputs(argsText);
        activity.Properties["Inputs"] = inputs;
        return activity;
    }

    private static IActivity CreateGenericActivity(string name, string argsText)
    {
        var activity = new DynamicActivity
        {
            Type = name,
            ExecuteHandler = _ => ValueTask.CompletedTask
        };
        var inputs = ParseActivityInputs(argsText);
        activity.Properties["Inputs"] = inputs;
        return activity;
    }

    private static Dictionary<string, string> ParseActivityInputs(string argsText)
    {
        var inputs = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(argsText))
            return inputs;

        // Named: Name: value, Positional: value, Inline: => code
        var args = SplitTopLevel(argsText, ',');
        var positionalIndex = 0;
        foreach (var arg in args)
        {
            var p = arg.Trim();
            var colon = p.IndexOf(':');
            if (colon > 0)
            {
                var aname = p.Substring(0, colon).Trim();
                var aval = p.Substring(colon + 1).Trim();
                inputs[aname] = aval;
            }
            else
            {
                var key = $"arg{positionalIndex++}";
                inputs[key] = p;
            }
        }
        return inputs;
    }

    private static Expression ParseExpressionFromValue(string text)
    {
        text = text.Trim();

        // Inline expression: => code
        if (text.StartsWith("=>"))
            return new Expression("JavaScript", text.Substring(2).Trim());

        // String literal
        if (text.StartsWith("\"") && text.EndsWith("\""))
            return new Expression("Literal", text.Substring(1, text.Length - 2));

        // Boolean literals
        if (text.Equals("true", StringComparison.OrdinalIgnoreCase))
            return new Expression("Literal", true);
        if (text.Equals("false", StringComparison.OrdinalIgnoreCase))
            return new Expression("Literal", false);

        // Number literal
        if (double.TryParse(text, out var n))
            return new Expression("Literal", n);

        // Default: treat as variable reference (JavaScript expression)
        return new Expression("JavaScript", text);
    }

    private static string TrimArrow(string code)
    {
        var c = code.Trim();
        if (c.StartsWith("=>")) c = c.Substring(2).Trim();
        return c;
    }

    private static int FindMatchingParen(string s, int openIndex)
    {
        return FindMatching(s, openIndex, '(', ')');
    }

    private static int FindMatchingBrace(string s, int openIndex)
    {
        return FindMatching(s, openIndex, '{', '}');
    }

    private static int FindMatching(string s, int openIndex, char openChar, char closeChar)
    {
        if (openIndex < 0 || openIndex >= s.Length || s[openIndex] != openChar) return -1;
        var depth = 0;
        var inSingle = false; var inDouble = false; var inBacktick = false;
        for (var i = openIndex; i < s.Length; i++)
        {
            var c = s[i];
            if (c == '\\') { i++; continue; }
            if (!inSingle && !inDouble && !inBacktick)
            {
                if (c == openChar) depth++;
                else if (c == closeChar)
                {
                    depth--;
                    if (depth == 0) return i;
                }
                else if (c == '\'') inSingle = true;
                else if (c == '"') inDouble = true;
                else if (c == '`') inBacktick = true;
            }
            else
            {
                if (inSingle && c == '\'') inSingle = false;
                else if (inDouble && c == '"') inDouble = false;
                else if (inBacktick && c == '`') inBacktick = false;
            }
        }
        return -1;
    }

    private static List<string> SplitTopLevel(string s, char delimiter)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var depthParen = 0; var depthBrace = 0; var depthBracket = 0;
        var inSingle = false; var inDouble = false; var inBacktick = false;
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c == '\\') { if (i + 1 < s.Length) { sb.Append(c); sb.Append(s[i + 1]); i++; } continue; }
            if (!inSingle && !inDouble && !inBacktick)
            {
                if (c == '(') depthParen++; else if (c == ')') depthParen--;
                else if (c == '{') depthBrace++; else if (c == '}') depthBrace--;
                else if (c == '[') depthBracket++; else if (c == ']') depthBracket--;
                else if (c == '\'') inSingle = true; else if (c == '"') inDouble = true; else if (c == '`') inBacktick = true;
                if (c == delimiter && depthParen == 0 && depthBrace == 0 && depthBracket == 0)
                { result.Add(sb.ToString()); sb.Clear(); continue; }
            }
            else
            {
                if (inSingle && c == '\'') inSingle = false;
                else if (inDouble && c == '"') inDouble = false;
                else if (inBacktick && c == '`') inBacktick = false;
            }
            sb.Append(c);
        }
        if (sb.Length > 0) result.Add(sb.ToString());
        return result;
    }
}
