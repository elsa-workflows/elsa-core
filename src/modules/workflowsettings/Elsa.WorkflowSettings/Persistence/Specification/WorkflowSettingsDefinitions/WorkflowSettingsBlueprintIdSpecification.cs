using System;
using System.Linq.Expressions;
using Elsa.Persistence.Specifications;
using Elsa.WorkflowSettings.Models;

namespace Elsa.WorkflowSettings.Persistence.Specification.WorkflowSettingsDefinitions
{
    public class WorkflowSettingsBlueprintIdSpecification : Specification<WorkflowSetting>
    {
        public string WorkflowBlueprintId { get; set; }

        public WorkflowSettingsBlueprintIdSpecification(string workflowBlueprintId) => WorkflowBlueprintId = workflowBlueprintId;

        public override Expression<Func<WorkflowSetting, bool>> ToExpression() => x => x.WorkflowBlueprintId == WorkflowBlueprintId;
    }
}