using Elsa.Workflows.Core.Activities.Flowchart.Handlers;
using Elsa.Workflows.Core.Activities.Flowchart.Services;
using Elsa.Workflows.Core.Activities.Flowchart.Transposition.Services;

namespace Elsa.Workflows.Core.Activities.Flowchart.Implementations;

public class TransposeHandlerRegistry : ITransposeHandlerRegistry
{
    private readonly IDictionary<string, Func<ITransposeHandler>> _dictionary = new Dictionary<string, Func<ITransposeHandler>>();
    public void Add(string activityType, Func<ITransposeHandler> handlerFactory) => _dictionary[activityType] = handlerFactory;
    public bool TryGet(string activityType, out Func<ITransposeHandler> handler) => _dictionary.TryGetValue(activityType, out handler!);
    public ITransposeHandler Create(string activityType) => TryGet(activityType, out var h) ? h() : new DefaultTransposeHandler();
}