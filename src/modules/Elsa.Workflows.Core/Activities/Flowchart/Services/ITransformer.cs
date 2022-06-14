namespace Elsa.Workflows.Core.Activities.Flowchart.Services;
using Flowchart = Activities.Flowchart;

/// <summary>
/// Transforms the specified <see cref="Flowchart"/> into a sequential structure by transposing connected activities into outbound activity properties of the source activity.
/// </summary>
public interface ITransformer
{
    void Transpose(Flowchart flowchart);
}