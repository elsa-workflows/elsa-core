using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Elsa.Api.Endpoints.TriggerDescriptors;

public class TriggerDescriptorsResource
{
    private readonly IEndpointRouteBuilder _endpoints;

    public TriggerDescriptorsResource(IEndpointRouteBuilder endpoints)
    {
        _endpoints = endpoints;
    }

    public TriggerDescriptorsResource List()
    {
        _endpoints.MapGet("api/descriptors/triggers", TriggerDescriptors.ListAsync);
        return this;
    }
}

public static class TriggerDescriptorsResourceExtensions
{
    public static IEndpointRouteBuilder MapTriggerDescriptors(this IEndpointRouteBuilder endpoints, Action<TriggerDescriptorsResource> configureResource)
    {
        configureResource(new(endpoints));
        return endpoints;
    }

    public static IEndpointRouteBuilder MapTriggerDescriptors(this IEndpointRouteBuilder endpoints) => endpoints.MapTriggerDescriptors(x => x.List());
}