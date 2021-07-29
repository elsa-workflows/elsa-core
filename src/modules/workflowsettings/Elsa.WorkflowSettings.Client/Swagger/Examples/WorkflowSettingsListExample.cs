using System.Collections.Generic;
using System.Linq;
using Elsa.WorkflowSettings.Models;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.WorkflowSettings.Client.Swagger.Examples
{
    public class WorkflowSettingsListExample : IExamplesProvider<IEnumerable<WorkflowSetting>>
    {
        public IEnumerable<WorkflowSetting> GetExamples()
        {
            var result = Enumerable.Range(1, 3).Select(_ => new WorkflowSettingsExample().GetExamples()).ToList();
            return result;
        }
    }
}