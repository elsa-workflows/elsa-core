namespace Elsa.Builders
{
    public static class WorkflowBuilderExtensions
    {
        public static Activities.Containers.Flowchart Build<T>(this IFlowchartBuilder builder) where T : IFlowchart, new()
        {
            var workflow = new T();
            builder.WithId(typeof(T).Name);
            workflow.Build(builder);

            return builder.Build();
        }
    }
}