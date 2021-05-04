using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

namespace Elsa.Activities.ControlFlow
{
    public class SwitchCaseBuilder
    {
        public ICollection<SwitchCaseDescriptor> Cases { get; } = new List<SwitchCaseDescriptor>();
        
        public SwitchCaseBuilder Add(string name, Func<ActivityExecutionContext, ValueTask<bool>> condition, Action<IOutcomeBuilder> branch)
        {
            Cases.Add(new SwitchCaseDescriptor(name, condition, builder => branch(builder.When(name))));
            return this;
        }
        
        public SwitchCaseBuilder Add(Func<ActivityExecutionContext, ValueTask<bool>> condition, Action<IOutcomeBuilder> branch)
        {
            var caseLabel = GetNextCaseLabel();
            Cases.Add(new SwitchCaseDescriptor(caseLabel, condition, builder => branch(builder.When(caseLabel))));
            return this;
        }

        public SwitchCaseBuilder Add(string name, Func<ActivityExecutionContext, bool> condition, Action<IOutcomeBuilder> branch) => Add(name, context => new ValueTask<bool>(condition(context)), branch);
        public SwitchCaseBuilder Add(string name, Func<ValueTask<bool>> condition, Action<IOutcomeBuilder> branch) => Add(name, _ => condition(), branch);
        public SwitchCaseBuilder Add(string name, Func<bool> condition, Action<IOutcomeBuilder> branch) => Add(name, _ => condition(), branch);
        public SwitchCaseBuilder Add(string name, bool condition, Action<IOutcomeBuilder> branch) => Add(name, _ => condition, branch);
        public SwitchCaseBuilder Add(Func<ActivityExecutionContext, bool> condition, Action<IOutcomeBuilder> branch) => Add(GetNextCaseLabel(), context => new ValueTask<bool>(condition(context)), branch);
        public SwitchCaseBuilder Add(Func<ValueTask<bool>> condition, Action<IOutcomeBuilder> branch) => Add(GetNextCaseLabel(), _ => condition(), branch);
        public SwitchCaseBuilder Add(Func<bool> condition, Action<IOutcomeBuilder> branch) => Add(GetNextCaseLabel(), _ => condition(), branch);
        public SwitchCaseBuilder Add(bool condition, Action<IOutcomeBuilder> branch) => Add(GetNextCaseLabel(), _ => condition, branch);

        private string GetNextCaseLabel() => $"Case {Cases.Count + 1}";
    }

    public record SwitchCaseDescriptor(string Name, Func<ActivityExecutionContext, ValueTask<bool>> Condition, Action<IActivityBuilder> OutcomeBuilder);
}