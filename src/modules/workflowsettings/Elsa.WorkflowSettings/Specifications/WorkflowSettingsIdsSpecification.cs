using Elsa.WorkflowSettings.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Elsa.Persistence.Specifications.WorkflowSettings
{
    public class WorkflowSettingsIdsSpecification : Specification<WorkflowSetting>
    {
        public WorkflowSettingsIdsSpecification(IEnumerable<string> ids) => Ids = ids;
        public IEnumerable<string> Ids { get; }
        public override Expression<Func<WorkflowSetting, bool>> ToExpression() => bookmark => Ids.Contains(bookmark.Id);
    }
}
