using AutoMapper;
using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Models;

namespace Elsa.Samples.AspNet.HttpEndpoints.Activities;

public class MapTo<TSource, TDestination> : CodeActivity<TDestination>
{
    public Input<TSource> Source { get; set; } = default!;

    protected override void Execute(ActivityExecutionContext context)
    {
        var mapper = context.GetRequiredService<IMapper>();
        var source = Source.Get(context);
        var destination = mapper.Map<TDestination>(source);
        context.SetResult(destination);
    }
}