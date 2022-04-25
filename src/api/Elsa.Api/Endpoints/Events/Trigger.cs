using Elsa.Api.ApiResults;
using Elsa.Modules.Activities.Primitives;
using Elsa.Runtime.Models;
using Elsa.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Api.Endpoints.Events;

public static class Trigger
{
    public static IResult Handle(string eventName, IHasher hasher)
    {
        var hash = hasher.Hash(eventName);
        var stimulus = Stimulus.Standard(nameof(Event), hash);
        return new ProcessStimulusResult(stimulus);
    }
}