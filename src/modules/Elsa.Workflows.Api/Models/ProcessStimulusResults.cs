using System.Collections.Generic;
using Elsa.AspNetCore.Models;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Api.Models;

public record ProcessStimulusResults(ICollection<ExecuteWorkflowInstructionResult> Items) : ListModel<ExecuteWorkflowInstructionResult>(Items);