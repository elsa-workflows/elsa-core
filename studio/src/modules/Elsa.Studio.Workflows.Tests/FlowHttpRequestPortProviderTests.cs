using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.ActivityPortProviders.Providers;
using Elsa.Studio.Workflows.Domain.Contexts;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="FlowHttpRequestPortProvider"/> matching for FlowSendHttpRequest
/// and subclasses that register under a different type name.
/// </summary>
public class FlowHttpRequestPortProviderTests
{
    private readonly FlowHttpRequestPortProvider _provider = new();

    [Fact]
    public void GetSupportsActivityType_IsTrue_ForFlowSendHttpRequest()
    {
        var context = CreateContext("Elsa.FlowSendHttpRequest");

        Assert.True(_provider.GetSupportsActivityType(context));
    }

    [Fact]
    public void GetSupportsActivityType_IsTrue_ForSubclassWithWrappedExpectedStatusCodes()
    {
        var context = CreateContext("MyApp.MyFlowSendHttpRequest", wrappedExpectedStatusCodes: true);

        Assert.True(_provider.GetSupportsActivityType(context));
    }

    [Fact]
    public void GetSupportsActivityType_IsFalse_ForSendHttpRequest()
    {
        var context = CreateContext("Elsa.SendHttpRequest", wrappedExpectedStatusCodes: false, includeExpectedStatusCodes: true);

        Assert.False(_provider.GetSupportsActivityType(context));
    }

    [Fact]
    public void GetSupportsActivityType_IsFalse_ForUnrelatedActivity()
    {
        var context = CreateContext("Elsa.WriteLine");

        Assert.False(_provider.GetSupportsActivityType(context));
    }

    [Fact]
    public void GetPorts_YieldsTheSameOutcomes_ForASubclassAsForFlowSendHttpRequest()
    {
        var basePorts = _provider.GetPorts(CreateContext("Elsa.FlowSendHttpRequest")).ToList();
        var subclassPorts = _provider.GetPorts(CreateContext("MyApp.MyFlowSendHttpRequest", wrappedExpectedStatusCodes: true)).ToList();

        Assert.Equal(["200", "Unmatched status code", "Failed to connect", "Timeout", "Done"], basePorts.Select(x => x.Name));
        Assert.Equal(basePorts.Select(x => x.Name), subclassPorts.Select(x => x.Name));
        Assert.All(subclassPorts, port => Assert.Equal(PortType.Flow, port.Type));
    }

    private static PortProviderContext CreateContext(
        string typeName,
        bool wrappedExpectedStatusCodes = false,
        bool includeExpectedStatusCodes = false)
    {
        var inputs = new List<InputDescriptor>();

        if (wrappedExpectedStatusCodes || includeExpectedStatusCodes)
        {
            inputs.Add(new InputDescriptor
            {
                Name = "ExpectedStatusCodes",
                TypeName = wrappedExpectedStatusCodes ? typeof(ICollection<int>).FullName! : "Elsa.Http.Models.HttpStatusCodeCase",
                IsWrapped = wrappedExpectedStatusCodes,
                UIHint = wrappedExpectedStatusCodes ? "multitext" : "http-status-codes"
            });
        }

        var descriptor = new ActivityDescriptor
        {
            TypeName = typeName,
            Name = typeName,
            Version = 1,
            Inputs = inputs
        };

        var activity = new JsonObject
        {
            ["id"] = "activity-1",
            ["type"] = typeName,
            ["version"] = 1
        };

        return new(descriptor, activity);
    }
}
