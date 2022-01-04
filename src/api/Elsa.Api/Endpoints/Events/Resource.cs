using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Elsa.Api.Endpoints.Events;

public class EventsResource
{
    private readonly IEndpointRouteBuilder _endpoints;

    public EventsResource(IEndpointRouteBuilder endpoints)
    {
        _endpoints = endpoints;
    }

    public EventsResource Trigger()
    {
        _endpoints.MapPost("api/events/{eventName}/trigger", Events.Trigger.Handle);
        return this;
    }
}

public static class EventsResourceExtensions
{
    public static IEndpointRouteBuilder MapEvents(this IEndpointRouteBuilder endpoints, Action<EventsResource> configureResource)
    {
        configureResource(new(endpoints));
        return endpoints;
    }

    public static IEndpointRouteBuilder MapEvents(this IEndpointRouteBuilder endpoints) => endpoints.MapEvents(x => x.Trigger());
}