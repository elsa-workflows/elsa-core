using Elsa.Workflows.Core.Activities.Flowchart.Transposition.Services;

namespace Elsa.Workflows.Core.Activities.Flowchart.Services;

public interface ITransposeHandlerRegistry
{
    void Add(string activityType, Func<ITransposeHandler> handlerFactory);
    bool TryGet(string activityType, out Func<ITransposeHandler>? handler);
    ITransposeHandler Create(string activityType);
}