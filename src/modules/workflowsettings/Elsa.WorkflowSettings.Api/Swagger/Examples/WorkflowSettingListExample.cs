using System.Collections.Generic;
using System.Linq;
using Elsa.WorkflowSettings.Models;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.WorkflowSettings.Api.Swagger.Examples
{
    public class WorkflowSettingListExample : IExamplesProvider<IEnumerable<WorkflowSetting>>
    {
        public IEnumerable<WorkflowSetting> GetExamples()
        {
            var result = Enumerable.Range(1, 3).Select(_ => new WorkflowSettingExample().GetExamples()).ToList();
            return result;
        }
    }
}