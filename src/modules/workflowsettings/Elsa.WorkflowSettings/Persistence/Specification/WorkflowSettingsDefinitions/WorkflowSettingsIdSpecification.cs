using System;
using System.Linq.Expressions;
using Elsa.Persistence.Specifications;
using Elsa.WorkflowSettings.Models;

namespace Elsa.WorkflowSettings.Persistence.Specification.WorkflowSettingsDefinitions
{
    public class WorkflowSettingsIdSpecification : Specification<WorkflowSetting>
    {
        public string Id { get; set; }

        public WorkflowSettingsIdSpecification(string id)
        {
            Id = id;
        }

        public override Expression<Func<WorkflowSetting, bool>> ToExpression() => x => x.Id == Id;
    }
}