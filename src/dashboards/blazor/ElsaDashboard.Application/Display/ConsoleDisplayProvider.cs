using System.Collections.Generic;
using ElsaDashboard.Application.Activities.Console;
using ElsaDashboard.Models;
using ElsaDashboard.Services;

namespace ElsaDashboard.Application.Display
{
    public class ConsoleDisplayProvider : ActivityDisplayProvider
    {
        protected override IEnumerable<ActivityDisplayDescriptor> GetDescriptors()
        {
            yield return ActivityDisplayDescriptor.For<WriteLineActivity>("WriteLine");
        }
    }
}