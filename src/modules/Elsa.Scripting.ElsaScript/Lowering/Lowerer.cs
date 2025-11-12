using System.Collections.Generic;
using System.Linq;
using Elsa.Scripting.ElsaScript.Ast;

namespace Elsa.Scripting.ElsaScript.Lowering;

public record WfSequence(IList<object> Activities);
public record WfActivity(string Type, IDictionary<string, object?> Inputs, string? Id = null, bool CanStart = false);
public record WfSwitchCase(object Value, WfSequence Body);

public class Lowerer
{
    public object Lower(ProgramNode program)
    {
        var activities = new List<object>();
        foreach (var member in program.Members)
        {
            LowerNode(member, activities, program.DefaultExpressionLanguage);
        }
        return new WfSequence(activities);
    }

    private void LowerNode(Node node, IList<object> container, string defaultLanguage)
    {
        switch (node)
        {
            case ActivityStmt activityStmt:
                container.Add(LowerActivity(activityStmt, defaultLanguage));
                break;
            case BlockNode blockNode:
                var blockActivities = new List<object>();
                foreach (var statement in blockNode.Statements)
                    LowerNode(statement, blockActivities, defaultLanguage);
                container.Add(new WfSequence(blockActivities));
                break;
            case IfNode ifNode:
                var thenSeq = LowerAsSequence(ifNode.Then, defaultLanguage);
                var elseSeq = ifNode.Else != null ? LowerAsSequence(ifNode.Else, defaultLanguage) : null;
                container.Add(new WfActivity("If", new Dictionary<string, object?>
                {
                    ["Condition"] = LowerExpr(ifNode.Condition, defaultLanguage),
                    ["Then"] = thenSeq,
                    ["Else"] = elseSeq
                }));
                break;
            case ForEachNode forEachNode:
                container.Add(new WfActivity("ForEach", new Dictionary<string, object?>
                {
                    ["CurrentValue"] = forEachNode.VariableName,
                    ["Body"] = LowerAsSequence(forEachNode.Body, defaultLanguage),
                    ["Items"] = LowerExpr(forEachNode.Items, defaultLanguage)
                }));
                break;
            case ForNode forNode:
                container.Add(new WfActivity("For", new Dictionary<string, object?>
                {
                    ["Initializer"] = forNode.Initializer != null ? LowerInitializer(forNode.Initializer, defaultLanguage) : null,
                    ["Condition"] = forNode.Condition != null ? LowerExpr(forNode.Condition, defaultLanguage) : null,
                    ["Iterator"] = forNode.Iterator != null ? LowerExpr(forNode.Iterator, defaultLanguage) : null,
                    ["Body"] = LowerAsSequence(forNode.Body, defaultLanguage)
                }));
                break;
            case WhileNode whileNode:
                container.Add(new WfActivity("While", new Dictionary<string, object?>
                {
                    ["Condition"] = LowerExpr(whileNode.Condition, defaultLanguage),
                    ["Body"] = LowerAsSequence(whileNode.Body, defaultLanguage)
                }));
                break;
            case SwitchNode switchNode:
                container.Add(LowerSwitch(switchNode, defaultLanguage));
                break;
            case WorkflowNode workflowNode:
                var workflowSeq = LowerAsSequence(workflowNode.Body, defaultLanguage);
                container.Add(new WfActivity("Workflow", new Dictionary<string, object?>
                {
                    ["Name"] = workflowNode.Name,
                    ["Body"] = workflowSeq
                }));
                break;
            default:
                break;
        }
    }

    private object LowerSwitch(SwitchNode switchNode, string defaultLanguage)
    {
        var cases = switchNode.Cases
            .Select(x => new WfSwitchCase(LowerExpr(x.Value, defaultLanguage), LowerAsSequence(x.Body, defaultLanguage)))
            .ToList();
        var dict = new Dictionary<string, object?>
        {
            ["Expression"] = LowerExpr(switchNode.Expression, defaultLanguage),
            ["Cases"] = cases
        };
        if (switchNode.Default != null)
            dict["Default"] = LowerAsSequence(switchNode.Default.Body, defaultLanguage);
        return new WfActivity("Switch", dict);
    }

    private object? LowerInitializer(Node initializer, string defaultLanguage)
    {
        return initializer switch
        {
            VarDeclNode varDecl => new
            {
                Kind = "var",
                varDecl.Name,
                varDecl.TypeName,
                Initializer = varDecl.Initializer != null ? LowerExpr(varDecl.Initializer, defaultLanguage) : null
            },
            LetDeclNode letDecl => new
            {
                Kind = "let",
                letDecl.Name,
                letDecl.TypeName,
                Initializer = letDecl.Initializer != null ? LowerExpr(letDecl.Initializer, defaultLanguage) : null
            },
            ConstDeclNode constDecl => new
            {
                Kind = "const",
                constDecl.Name,
                constDecl.TypeName,
                Initializer = LowerExpr(constDecl.Initializer, defaultLanguage)
            },
            ExprStmt exprStmt => new
            {
                Kind = "expr",
                Value = LowerExpr(exprStmt.Expression, defaultLanguage)
            },
            _ => null
        };
    }

    private WfSequence LowerAsSequence(Node node, string defaultLanguage)
    {
        if (node is BlockNode block)
        {
            var list = new List<object>();
            foreach (var statement in block.Statements)
                LowerNode(statement, list, defaultLanguage);
            return new WfSequence(list);
        }

        var container = new List<object>();
        LowerNode(node, container, defaultLanguage);
        return new WfSequence(container);
    }

    private object LowerActivity(ActivityStmt activityStmt, string defaultLanguage)
    {
        var invocation = activityStmt.Invocation;
        var inputs = new Dictionary<string, object?>
        {
            ["Name"] = invocation.Name,
            ["Arguments"] = invocation.Arguments.Select(arg => new
            {
                arg.Name,
                Value = LowerExpr(arg.Value, defaultLanguage)
            }).ToList()
        };

        return new WfActivity(invocation.Name, inputs, invocation.Alias, invocation.Listen);
    }

    private object? LowerExpr(Expr expr, string defaultLanguage)
    {
        return expr switch
        {
            LiteralExpr literal => literal.Value,
            IdentifierExpr identifier => new { Type = "Identifier", Name = identifier.Name },
            TemplateStringExpr template => new { Type = "Template", Language = defaultLanguage, Parts = template.Parts },
            LambdaExpr lambda => new { Type = "Expression", Language = lambda.Language, Code = lambda.Code },
            RawExpr raw => new { Type = "Expression", Language = defaultLanguage, Code = raw.Code },
            _ => null
        };
    }
}
