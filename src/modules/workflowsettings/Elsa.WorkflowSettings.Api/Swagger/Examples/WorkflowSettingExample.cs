using System;
using Elsa.WorkflowSettings.Models;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.WorkflowSettings.Api.Swagger.Examples
{
    public class WorkflowSettingExample : IExamplesProvider<WorkflowSetting>
    {
        public WorkflowSetting GetExamples()
        {
            return new()
            {
                Id = Guid.NewGuid().ToString("N"),
                WorkflowBlueprintId = Guid.NewGuid().ToString("N"),
                Key = "Disabled",
                Value = "true"
            };
        }
    }
}