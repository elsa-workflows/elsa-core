using Elsa.Workflows.Core.Activities.Flowchart.Activities;
using Elsa.Workflows.Core.Activities.Flowchart.Models;

namespace Elsa.Workflows.Core.Activities.Flowchart.Transposition.Services;

public interface ITransposeHandler
{
    bool Transpose(TransposeContext context);
}