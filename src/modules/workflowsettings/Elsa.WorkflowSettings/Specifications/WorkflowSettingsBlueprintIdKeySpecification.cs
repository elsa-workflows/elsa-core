using Elsa.Persistence.Specifications;
using Elsa.WorkflowSettings.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Elsa.WorkflowSettings.Specifications
{
    public class WorkflowSettingsBlueprintIdKeySpecification : Specification<WorkflowSetting>
    {
        public string Key { get; set; }
        public string BlueprintId { get; set; }
        public WorkflowSettingsBlueprintIdKeySpecification(string key, string blueprintId) {
            Key = key;
            BlueprintId = blueprintId;
        }
        public override Expression<Func<WorkflowSetting, bool>> ToExpression() => x => x.Key == Key && x.WorkflowBlueprintId == BlueprintId;
    }
}