using Elsa.Dsl.ElsaScript.Ast;
using Elsa.Dsl.ElsaScript.Contracts;
using Elsa.Expressions.Models;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Dsl.ElsaScript.Compiler;

/// <summary>
/// Compiles an ElsaScript AST into an Elsa workflow.
/// </summary>
public class ElsaScriptCompiler : IElsaScriptCompiler
{
    private readonly IActivityRegistry _activityRegistry;
    private string _defaultExpressionLanguage = "JavaScript";
    private readonly Dictionary<string, Variable> _variables = new();

    public ElsaScriptCompiler(IActivityRegistry activityRegistry)
    {
        _activityRegistry = activityRegistry;
    }

    /// <inheritdoc />
    public Workflow Compile(WorkflowNode workflowNode)
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
            var activity = CompileStatement(statement);
            if (activity != null)
            {
                activities.Add(activity);
            }
        }

        // Create the root activity (Sequence containing all statements)
        var root = activities.Count == 1 
            ? activities[0] 
            : new Sequence { Activities = activities };

        // Create the workflow
        var workflow = new Workflow
        {
            Name = workflowNode.Name,
            Root = root,
            Variables = _variables.Values.ToList()
        };

        return workflow;
    }

    private IActivity? CompileStatement(StatementNode statement)
    {
        return statement switch
        {
            VariableDeclarationNode varDecl => CompileVariableDeclaration(varDecl),
            ActivityInvocationNode actInv => CompileActivityInvocation(actInv),
            BlockNode block => CompileBlock(block),
            IfNode ifNode => CompileIf(ifNode),
            ForEachNode forEach => CompileForEach(forEach),
            ForNode forNode => CompileFor(forNode),
            WhileNode whileNode => CompileWhile(whileNode),
            SwitchNode switchNode => CompileSwitch(switchNode),
            FlowchartNode flowchart => CompileFlowchart(flowchart),
            ListenNode listen => CompileListen(listen),
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

    private IActivity CompileActivityInvocation(ActivityInvocationNode actInv)
    {
        // Try to find the activity type by name
        var activityDescriptor = _activityRegistry.Find(actInv.ActivityName);
        
        if (activityDescriptor == null)
        {
            throw new InvalidOperationException($"Activity '{actInv.ActivityName}' not found in registry");
        }

        // Create an instance of the activity using a minimal constructor context
        var emptyElement = System.Text.Json.JsonDocument.Parse("{}").RootElement;
        var context = new ActivityConstructorContext(activityDescriptor, emptyElement, new System.Text.Json.JsonSerializerOptions());
        var activity = activityDescriptor.Constructor(context);

        // Set properties based on arguments
        var activityType = activity.GetType();
        
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

    private IActivity CompileBlock(BlockNode block)
    {
        var activities = new List<IActivity>();
        
        foreach (var statement in block.Statements)
        {
            var activity = CompileStatement(statement);
            if (activity != null)
            {
                activities.Add(activity);
            }
        }

        return new Sequence { Activities = activities };
    }

    private IActivity CompileIf(IfNode ifNode)
    {
        var condition = CompileExpressionAsInput<bool>(ifNode.Condition);
        var thenActivity = CompileStatement(ifNode.Then);
        var elseActivity = ifNode.Else != null ? CompileStatement(ifNode.Else) : null;

        return new If(condition)
        {
            Then = thenActivity,
            Else = elseActivity
        };
    }

    private IActivity CompileForEach(ForEachNode forEach)
    {
        // Create the loop variable
        var loopVariable = new Variable<object>(forEach.VariableName, null);
        _variables[forEach.VariableName] = loopVariable;

        var items = CompileExpressionAsInput<ICollection<object>>(forEach.Collection);
        var body = CompileStatement(forEach.Body);

        var forEachActivity = new ForEach<object>(items)
        {
            CurrentValue = new Output<object>(loopVariable),
            Body = body
        };

        return forEachActivity;
    }

    private IActivity CompileFor(ForNode forNode)
    {
        // For now, implement a simplified version using While
        // A full implementation would need to handle initializer and iterator properly
        throw new NotImplementedException("For loops are not yet fully implemented");
    }

    private IActivity CompileWhile(WhileNode whileNode)
    {
        var condition = CompileExpressionAsInput<bool>(whileNode.Condition);
        var body = CompileStatement(whileNode.Body);

        return new While(condition)
        {
            Body = body
        };
    }

    private IActivity CompileSwitch(SwitchNode switchNode)
    {
        var cases = new List<SwitchCase>();
        
        foreach (var caseNode in switchNode.Cases)
        {
            var caseExpression = CompileExpressionAsExpression(caseNode.Value);
            var caseBody = CompileStatement(caseNode.Body);
            cases.Add(new SwitchCase("Case", caseExpression, caseBody!));
        }

        var defaultActivity = switchNode.Default != null ? CompileStatement(switchNode.Default) : null;

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

    private IActivity CompileListen(ListenNode listen)
    {
        // Listen is just a regular activity invocation that can start a workflow
        var activity = CompileActivityInvocation(listen.Activity);
        
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
            return new Input<T>((T)literal.Value!);
        }

        if (exprNode is IdentifierNode identifier)
        {
            // Reference to a variable
            if (_variables.TryGetValue(identifier.Name, out var variable))
            {
                return new Input<T>(variable);
            }
            
            // If not found, treat as a literal
            return new Input<T>(new Literal<T>(default!));
        }

        if (exprNode is ElsaExpressionNode elsaExpr)
        {
            var language = elsaExpr.Language ?? _defaultExpressionLanguage;
            var expression = new Expression(language, elsaExpr.Expression);
            return new Input<T>(expression);
        }

        if (exprNode is ArrayLiteralNode arrayLiteral)
        {
            // For array literals, evaluate to a constant array if all elements are literals
            var elements = arrayLiteral.Elements.Select(EvaluateConstantExpression).ToArray();
            return new Input<T>((T)(object)elements);
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
            return new Expression(language, elsaExpr.Expression);
        }

        throw new NotSupportedException($"Expression type {exprNode.GetType().Name} is not supported as Expression");
    }

    private object CompileExpression(ExpressionNode exprNode, Type targetType)
    {
        // Determine the Input<T> type
        var inputType = typeof(Input<>).MakeGenericType(targetType);
        
        // Use reflection to call CompileExpressionAsInput<T>
        var method = GetType().GetMethod(nameof(CompileExpressionAsInput), 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var genericMethod = method!.MakeGenericMethod(targetType);
        
        return genericMethod.Invoke(this, new object[] { exprNode })!;
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
