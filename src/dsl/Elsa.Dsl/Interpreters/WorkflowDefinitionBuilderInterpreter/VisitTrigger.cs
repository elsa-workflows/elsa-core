using Elsa.Contracts;

namespace Elsa.Dsl.Interpreters;

public partial class WorkflowDefinitionBuilderInterpreter
{
        
    public override IWorkflowDefinitionBuilder VisitTrigger(ElsaParser.TriggerContext context)
    {
        VisitChildren(context);
        var trigger = (ITrigger)_object.Get(context.@object());

        _workflowDefinitionBuilder.AddTrigger(trigger);

        return DefaultResult;
    }

}