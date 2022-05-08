using System.Collections.Generic;
using Elsa.Runtime.Models;

namespace Elsa.Api.Models;

public record ProcessStimulusResults(ICollection<ExecuteWorkflowInstructionResult> Items) : ListModel<ExecuteWorkflowInstructionResult>(Items);