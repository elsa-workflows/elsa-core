using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Dsl.Interpreters;

public partial class WorkflowDefinitionBuilderInterpreter
{
    public override IWorkflowBuilder VisitNewObjectExpr(ElsaParser.NewObjectExprContext context)
    {
        VisitChildren(context);

        var value = _expressionValue.Get(context.newObject());
        _expressionValue.Put(context, value);

        return DefaultResult;
    }

    public override IWorkflowBuilder VisitNewObject(ElsaParser.NewObjectContext context)
    {
        var objectTypeName = context.ID().GetText();
        var typeArg = context.type()?.GetText();
        TypeDescriptor? typeArgTypeDescriptor = default;

        if (typeArg != null)
        {
            objectTypeName = $"{objectTypeName}<>";
            typeArgTypeDescriptor = _typeSystem.ResolveTypeName(typeArg) ?? throw new Exception($"Cannot use type {typeArg} as type argument because it was not found in the type system.");
        }

        var typeDescriptor = _typeSystem.ResolveTypeName(objectTypeName);

        if (typeDescriptor == null)
            throw new Exception($"Could not instantiate type {objectTypeName} because it was not found in the type system.");

        VisitChildren(context);

        var argValues = _argValues.Get(context.args()).ToArray();
        var objectType = typeDescriptor.Type;

        if (typeArgTypeDescriptor != null)
        {
            var objectTypeArg = typeArgTypeDescriptor.Type;
            objectType = objectType.MakeGenericType(objectTypeArg);
        }

        var objectInstance = Activator.CreateInstance(objectType, argValues);

        _expressionValue.Put(context, objectInstance);

        return DefaultResult;
    }
}