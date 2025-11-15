using System.Text.Json;
using Elsa.Dsl.ElsaScript.Ast;
using Elsa.Dsl.ElsaScript.Contracts;
using Elsa.Dsl.ElsaScript.Helpers;
using Elsa.Expressions.Models;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dsl.ElsaScript.Compiler;

/// <summary>
/// Compiles an ElsaScript AST into an Elsa workflow.
/// </summary>
public class ElsaScriptCompiler(IActivityRegistryLookupService activityRegistryLookupService, IElsaScriptParser parser) : IElsaScriptCompiler
{
    private string _defaultExpressionLanguage = "JavaScript";
    private readonly Dictionary<string, Variable> _variables = new();

    /// <inheritdoc />
    public Task<Workflow> CompileAsync(string source, CancellationToken cancellationToken = default)
    {
        var workflowNode = parser.Parse(source);
        return CompileAsync(workflowNode, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Workflow> CompileAsync(WorkflowNode workflowNode, CancellationToken cancellationToken = default)
    {
        _variables.Clear();

        // Process use statements
        foreach (var useNode in workflowNode.UseStatements)
        {
            if (useNode.Type == UseType.Expressions)
            {
                _defaultExpressionLanguage = MapLanguageName(useNode.Value);
            }
        }

        // Compile body statements
        var activities = new List<IActivity>();
        foreach (var statement in workflowNode.Body)
        {
            var activity = await CompileStatementAsync(statement, cancellationToken);
            if (activity != null)
            {
                activities.Add(activity);
            }
        }

        // Create the root activity (Sequence containing all statements)
        var root = activities.Count == 1
            ? activities[0]
            : new Sequence
            {
                Activities = activities
            };

        // Create the workflow
        var workflow = new Workflow
        {
            Name = workflowNode.Name,
            WorkflowMetadata = new(workflowNode.Name, workflowNode.Description, ToolVersion: new("3.6.0")),
            Root = root,
            Variables = _variables.Values.ToList()
        };

        return workflow;
    }

    private async Task<IActivity?> CompileStatementAsync(StatementNode statement, CancellationToken cancellationToken = default)
    {
        return statement switch
        {
            VariableDeclarationNode varDecl => CompileVariableDeclaration(varDecl),
            ActivityInvocationNode actInv => await CompileActivityInvocationAsync(actInv, cancellationToken),
            BlockNode block => await CompileBlockAsync(block, cancellationToken),
            IfNode ifNode => await CompileIfAsync(ifNode, cancellationToken),
            ForEachNode forEach => await CompileForEachAsync(forEach, cancellationToken),
            ForNode forNode => await CompileForAsync(forNode, cancellationToken),
            WhileNode whileNode => await CompileWhileAsync(whileNode, cancellationToken),
            SwitchNode switchNode => await CompileSwitchAsync(switchNode, cancellationToken),
            FlowchartNode flowchart => CompileFlowchart(flowchart),
            ListenNode listen => await CompileListenAsync(listen, cancellationToken),
            _ => throw new NotSupportedException($"Statement type {statement.GetType().Name} is not supported")
        };
    }

    private IActivity? CompileVariableDeclaration(VariableDeclarationNode varDecl)
    {
        // Create and register the variable
        var initialValue = varDecl.Value != null ? EvaluateConstantExpression(varDecl.Value) : null;
        var variable = new Variable(varDecl.Name, initialValue);
        _variables[varDecl.Name] = variable;

        // Variable declarations don't produce activities themselves
        return null;
    }

    private async Task<IActivity> CompileActivityInvocationAsync(ActivityInvocationNode actInv, CancellationToken cancellationToken = default)
    {
        // Try to find the activity type by name - try several strategies
        var activityDescriptor = await activityRegistryLookupService.FindAsync(actInv.ActivityName);

        // If not found, try with "Elsa." prefix
        if (activityDescriptor == null)
        {
            activityDescriptor = await activityRegistryLookupService.FindAsync($"Elsa.{actInv.ActivityName}");
        }

        // If still not found, search by descriptor name
        if (activityDescriptor == null)
        {
            activityDescriptor = await activityRegistryLookupService.FindAsync(d => d.Name == actInv.ActivityName);
        }

        if (activityDescriptor == null)
        {
            throw new InvalidOperationException($"Activity '{actInv.ActivityName}' not found in registry");
        }

        // Create an empty JsonElement representing an empty object:
        var createContext = new ActivityConstructorContext(activityDescriptor, ActivityActivator.Create);
        var activity = activityDescriptor.Constructor(createContext);
        var activityType = activity.GetType();

        // Set properties based on arguments
        foreach (var arg in actInv.Arguments)
        {
            var propertyName = arg.Name ?? GetDefaultPropertyName(actInv.ActivityName);

            if (propertyName != null)
            {
                var property = activityType.GetProperty(propertyName);

                if (property != null)
                {
                    var value = CompileExpression(arg.Value, property.PropertyType);
                    property.SetValue(activity, value);
                }
            }
        }

        return activity;
    }

    private async Task<IActivity> CompileBlockAsync(BlockNode block, CancellationToken cancellationToken = default)
    {
        var activities = new List<IActivity>();

        foreach (var statement in block.Statements)
        {
            var activity = await CompileStatementAsync(statement, cancellationToken);
            if (activity != null)
            {
                activities.Add(activity);
            }
        }

        return new Sequence
        {
            Activities = activities
        };
    }

    private async Task<IActivity> CompileIfAsync(IfNode ifNode, CancellationToken cancellationToken = default)
    {
        var condition = CompileExpressionAsInput<bool>(ifNode.Condition);
        var thenActivity = await CompileStatementAsync(ifNode.Then, cancellationToken);
        var elseActivity = ifNode.Else != null ? await CompileStatementAsync(ifNode.Else, cancellationToken) : null;

        return new If(condition)
        {
            Then = thenActivity,
            Else = elseActivity
        };
    }

    private async Task<IActivity> CompileForEachAsync(ForEachNode forEach, CancellationToken cancellationToken = default)
    {
        // Create the loop variable
        var loopVariable = new Variable<object>(forEach.VariableName, null);
        _variables[forEach.VariableName] = loopVariable;

        var items = CompileExpressionAsInput<ICollection<object>>(forEach.Collection);
        var body = await CompileStatementAsync(forEach.Body, cancellationToken);

        var forEachActivity = new ForEach<object>(items)
        {
            CurrentValue = new(loopVariable),
            Body = body
        };

        return forEachActivity;
    }

    private async Task<IActivity> CompileForAsync(ForNode forNode, CancellationToken cancellationToken = default)
    {
        // For now, implement a simplified version using While
        // A full implementation would need to handle initializer and iterator properly
        throw new NotImplementedException("For loops are not yet fully implemented");
    }

    private async Task<IActivity> CompileWhileAsync(WhileNode whileNode, CancellationToken cancellationToken = default)
    {
        var condition = CompileExpressionAsInput<bool>(whileNode.Condition);
        var body = await CompileStatementAsync(whileNode.Body, cancellationToken);

        return new While(condition)
        {
            Body = body
        };
    }

    private async Task<IActivity> CompileSwitchAsync(SwitchNode switchNode, CancellationToken cancellationToken = default)
    {
        var cases = new List<SwitchCase>();

        foreach (var caseNode in switchNode.Cases)
        {
            var caseExpression = CompileExpressionAsExpression(caseNode.Value);
            var caseBody = await CompileStatementAsync(caseNode.Body, cancellationToken);
            cases.Add(new("Case", caseExpression, caseBody!));
        }

        var defaultActivity = switchNode.Default != null ? await CompileStatementAsync(switchNode.Default, cancellationToken) : null;

        return new Switch
        {
            Cases = cases,
            Default = defaultActivity
        };
    }

    private IActivity CompileFlowchart(FlowchartNode flowchart)
    {
        throw new NotImplementedException("Flowchart support is not yet implemented");
    }

    private async Task<IActivity> CompileListenAsync(ListenNode listen, CancellationToken cancellationToken = default)
    {
        // Listen is just a regular activity invocation that can start a workflow
        var activity = await CompileActivityInvocationAsync(listen.Activity, cancellationToken);

        // Try to set CanStartWorkflow if the activity supports it
        var canStartWorkflowProp = activity.GetType().GetProperty("CanStartWorkflow");
        if (canStartWorkflowProp != null && canStartWorkflowProp.PropertyType == typeof(bool))
        {
            canStartWorkflowProp.SetValue(activity, true);
        }

        return activity;
    }

    private Input<T> CompileExpressionAsInput<T>(ExpressionNode exprNode)
    {
        if (exprNode is LiteralNode literal)
        {
            return new(new Literal(literal.Value!));
        }

        if (exprNode is IdentifierNode identifier)
        {
            // Reference to a variable
            if (_variables.TryGetValue(identifier.Name, out var variable))
            {
                return new(variable);
            }

            // If not found, treat as a literal
            return new(new Literal<T>(default!));
        }

        if (exprNode is ElsaExpressionNode elsaExpr)
        {
            var language = elsaExpr.Language ?? _defaultExpressionLanguage;
            var expression = new Expression(language, elsaExpr.Expression);
            return new(expression);
        }

        if (exprNode is ArrayLiteralNode arrayLiteral)
        {
            // For array literals, evaluate to a constant array if all elements are literals
            var elements = arrayLiteral.Elements.Select(EvaluateConstantExpression).ToArray();
            return new((T)(object)elements);
        }

        throw new NotSupportedException($"Expression type {exprNode.GetType().Name} is not supported");
    }

    private Expression CompileExpressionAsExpression(ExpressionNode exprNode)
    {
        if (exprNode is LiteralNode literal)
        {
            return Expression.LiteralExpression(literal.Value);
        }

        if (exprNode is ElsaExpressionNode elsaExpr)
        {
            var language = elsaExpr.Language ?? _defaultExpressionLanguage;
            return new(language, elsaExpr.Expression);
        }

        throw new NotSupportedException($"Expression type {exprNode.GetType().Name} is not supported as Expression");
    }

    private object CompileExpression(ExpressionNode exprNode, Type targetType)
    {
        // Check if targetType is already Input<T>
        Type innerType;
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Input<>))
        {
            // Extract the T from Input<T>
            innerType = targetType.GetGenericArguments()[0];
        }
        else
        {
            innerType = targetType;
        }

        // Use reflection to call CompileExpressionAsInput<T>
        var method = GetType().GetMethod(nameof(CompileExpressionAsInput),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var genericMethod = method!.MakeGenericMethod(innerType);

        return genericMethod.Invoke(this, [exprNode])!;
    }

    private object? EvaluateConstantExpression(ExpressionNode exprNode)
    {
        if (exprNode is LiteralNode literal)
        {
            return literal.Value;
        }

        if (exprNode is ArrayLiteralNode arrayLiteral)
        {
            return arrayLiteral.Elements.Select(EvaluateConstantExpression).ToArray();
        }

        // For non-constant expressions, return null
        return null;
    }

    private string? GetDefaultPropertyName(string activityName)
    {
        // Map common activity names to their primary property
        return activityName switch
        {
            "WriteLine" => "Text",
            "WriteHttpResponse" => "Content",
            "SendEmail" => "Body",
            "HttpEndpoint" => "Path",
            _ => null
        };
    }

    private string MapLanguageName(string dslLanguage)
    {
        return dslLanguage.ToLowerInvariant() switch
        {
            "js" => "JavaScript",
            "cs" => "C#",
            "py" => "Python",
            "liquid" => "Liquid",
            _ => dslLanguage
        };
    }
}