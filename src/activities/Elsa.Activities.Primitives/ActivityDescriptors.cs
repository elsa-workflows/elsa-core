using System.Collections.Generic;
using System.Linq;
using Elsa.Activities.Primitives.Activities;
using Elsa.Models;
using Microsoft.Extensions.Localization;

namespace Elsa.Activities.Primitives
{
    public class ActivityDescriptors : ActivityDescriptorProviderBase
    {
        public ActivityDescriptors(IStringLocalizer<ActivityDescriptors> localizer)
        {
            T = localizer;
        }

        private IStringLocalizer<ActivityDescriptors> T { get; }
        private LocalizedString PrimitivesFlowCategory => T["Primitives"];
        private LocalizedString ControlFlowCategory => T["Control Flow"];

        protected override IEnumerable<ActivityDescriptor> Describe()
        {
            yield return ActivityDescriptor.ForAction<ForEach>(
                ControlFlowCategory,
                T["For Each"],
                T["Iterate over a list of items."],
                T["Next"], T["Done"]);
            
            yield return ActivityDescriptor.For<Fork>(
                ControlFlowCategory,
                T["Fork"],
                T["Fork workflow execution into separate paths of execution."],
                false,
                true,
                a => a.Forks.Select(x => T[x]));
            
            yield return ActivityDescriptor.ForAction<Join>(
                ControlFlowCategory,
                T["Join"],
                T["Join workflow execution back into a single path of execution."],
                T["Done"]);
            
            yield return ActivityDescriptor.ForAction<IfElse>(
                ControlFlowCategory,
                T["If/Else"],
                T["Evaluate a boolean condition and continues execution based on the outcome."],
                T["True"], T["False"]);
            
            yield return ActivityDescriptor.ForAction<SetVariable>(
                PrimitivesFlowCategory,
                T["Set Variable"],
                T["Set a custom variable on the workflow."],
                T["Done"]);
            
            yield return ActivityDescriptor.For<Switch>(
                ControlFlowCategory,
                T["Switch"],
                T["Execute a branch based on the outcome of a given condition."],
                false,
                true,
                a => a.Cases.Select(x => T[x]));
        }
    }
}