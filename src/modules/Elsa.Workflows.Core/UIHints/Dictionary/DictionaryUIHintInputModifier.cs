using Elsa.Workflows.Models;

namespace Elsa.Workflows.UIHints.Dictionary;

public class DictionaryUIHintInputModifier : IActivityDescriptorModifier
{
    public void Modify(ActivityDescriptor descriptor)
    {
        var dictionaryInputs = descriptor.Inputs.Where(x => x.UIHint == InputUIHints.Dictionary).ToList();
        
        foreach (var dictionaryInput in dictionaryInputs)
            dictionaryInput.EvaluatorType = typeof(DictionaryValueEvaluator);
    }
}