using Elsa.Contracts;
using Elsa.Dsl.Models;

namespace Elsa.Dsl.Interpreters;

public partial class WorkflowDefinitionBuilderInterpreter
{
    public override IWorkflowDefinitionBuilder VisitLocalVarDecl(ElsaParser.LocalVarDeclContext context)
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