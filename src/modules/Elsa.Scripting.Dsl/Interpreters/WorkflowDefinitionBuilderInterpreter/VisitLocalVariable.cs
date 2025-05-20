using Elsa.Scripting.Dsl.Models;
using Elsa.Workflows;

namespace Elsa.Scripting.Dsl.Interpreters;

public partial class WorkflowDefinitionBuilderInterpreter
{
    public override IWorkflowBuilder VisitLocalVarDecl(ElsaParser.LocalVarDeclContext context)
    {
        var variableName = context.ID().GetText();
        var variableType = context.type()?.ID().GetText();

        VisitChildren(context);

        var value = _expressionValue.Get(context.expr());

        var variable = new DefinedVariable
        {
            Identifier = variableName,
            Value = value
        };

        _definedVariables[variableName] = variable;

        return DefaultResult;
    }
}