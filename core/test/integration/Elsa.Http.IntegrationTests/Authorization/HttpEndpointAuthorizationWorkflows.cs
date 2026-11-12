using System.Net;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;
using Microsoft.AspNetCore.Http;
using Endpoint = Elsa.Workflows.Activities.Flowchart.Models.Endpoint;

namespace Elsa.Http.IntegrationTests.Authorization;

/// <summary>
/// Two HttpEndpoint stages: the first starts the workflow, the second one waits for a follow-up request.
/// </summary>
public abstract class TwoStageHttpWorkflow : WorkflowBase
{
    protected abstract string Name { get; }
    protected abstract HttpEndpoint CreateStageOne(string path);
    protected abstract HttpEndpoint CreateStageTwo(string path);

    /// <summary>An optional activity that runs between the second stage and the final response.</summary>
    protected virtual IActivity? CreateAfterStageTwo() => null;

    public string StartPath => $"{Name}/start";
    public string ResumePath => $"{Name}/resume";

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(Name);

        var stageOne = CreateStageOne(StartPath);
        var stageTwo = CreateStageTwo(ResumePath);
        stageOne.CanStartWorkflow = true;
        stageTwo.CanStartWorkflow = false;

        var done = new WriteHttpResponse
        {
            Content = new("done"),
            ContentType = new("text/plain"),
            StatusCode = new(HttpStatusCode.OK)
        };

        var after = CreateAfterStageTwo();

        var flowchart = new Flowchart
        {
            Start = stageOne,
            Activities = { stageOne, stageTwo, done },
            Connections =
            {
                new Connection(new Endpoint(stageOne, "Done"), new Endpoint(stageTwo)),
                new Connection(new Endpoint(stageTwo, "Done"), new Endpoint(after ?? done))
            }
        };

        if (after != null)
        {
            flowchart.Activities.Add(after);
            flowchart.Connections.Add(new Connection(new Endpoint(after, "Done"), new Endpoint(done)));
        }

        builder.Root = flowchart;
    }

    protected static HttpEndpoint CreateEndpoint(string path, bool authorize, string? policy = null, params string[] methods) => new()
    {
        Path = new(path),
        SupportedMethods = new(methods.Length > 0 ? methods : [HttpMethods.Post]),
        Authorize = new(authorize),
        Policy = new(policy)
    };
}

/// <summary>Both stages require an authenticated user.</summary>
public class AuthorizeWorkflow : TwoStageHttpWorkflow
{
    protected override string Name => "authorize";
    protected override HttpEndpoint CreateStageOne(string path) => CreateEndpoint(path, true);
    protected override HttpEndpoint CreateStageTwo(string path) => CreateEndpoint(path, true);
}

/// <summary>Both stages require an authenticated user that satisfies a policy.</summary>
public class PolicyWorkflow : TwoStageHttpWorkflow
{
    public const string PolicyName = "Admins";
    protected override string Name => "policy";
    protected override HttpEndpoint CreateStageOne(string path) => CreateEndpoint(path, true, PolicyName);
    protected override HttpEndpoint CreateStageTwo(string path) => CreateEndpoint(path, true, PolicyName);
}

/// <summary>Only the second stage is protected, and it also sets a timeout and a size limit.</summary>
public class SecondStageProtectedWorkflow : TwoStageHttpWorkflow
{
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    public const long SizeLimit = 1024;

    protected override string Name => "second-stage";
    protected override HttpEndpoint CreateStageOne(string path) => CreateEndpoint(path, false);

    protected override HttpEndpoint CreateStageTwo(string path)
    {
        var endpoint = CreateEndpoint(path, true, PolicyWorkflow.PolicyName);
        endpoint.RequestTimeout = new(Timeout);
        endpoint.RequestSizeLimit = new(SizeLimit);
        return endpoint;
    }
}

/// <summary>Both stages require a policy, and the second stage accepts two methods.</summary>
public class MultiMethodWorkflow : TwoStageHttpWorkflow
{
    protected override string Name => "multi-method";
    protected override HttpEndpoint CreateStageOne(string path) => CreateEndpoint(path, true, PolicyWorkflow.PolicyName);
    protected override HttpEndpoint CreateStageTwo(string path) => CreateEndpoint(path, true, PolicyWorkflow.PolicyName, HttpMethods.Get, HttpMethods.Post);
}

/// <summary>Neither stage is protected.</summary>
public class AnonymousWorkflow : TwoStageHttpWorkflow
{
    protected override string Name => "anonymous";
    protected override HttpEndpoint CreateStageOne(string path) => CreateEndpoint(path, false);
    protected override HttpEndpoint CreateStageTwo(string path) => CreateEndpoint(path, false);
}

/// <summary>A policy is set, but Authorize is off.</summary>
public class PolicyWithoutAuthorizeWorkflow : TwoStageHttpWorkflow
{
    protected override string Name => "policy-without-authorize";
    protected override HttpEndpoint CreateStageOne(string path) => CreateEndpoint(path, false, PolicyWorkflow.PolicyName);
    protected override HttpEndpoint CreateStageTwo(string path) => CreateEndpoint(path, false, PolicyWorkflow.PolicyName);
}

/// <summary>The second stage sets a short request timeout and is followed by a slow activity.</summary>
public class TimeoutWorkflow : TwoStageHttpWorkflow
{
    public static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(300);
    public static readonly TimeSpan SlowActivityDuration = TimeSpan.FromSeconds(20);

    protected override string Name => "timeout";
    protected override HttpEndpoint CreateStageOne(string path) => CreateEndpoint(path, false);

    protected override HttpEndpoint CreateStageTwo(string path)
    {
        var endpoint = CreateEndpoint(path, false);
        endpoint.RequestTimeout = new(Timeout);
        return endpoint;
    }

    protected override IActivity CreateAfterStageTwo() => new SlowActivity();
}

/// <summary>Waits until it is cancelled or the duration has passed.</summary>
public class SlowActivity : CodeActivity
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        await Task.Delay(TimeoutWorkflow.SlowActivityDuration, context.CancellationToken);
    }
}
