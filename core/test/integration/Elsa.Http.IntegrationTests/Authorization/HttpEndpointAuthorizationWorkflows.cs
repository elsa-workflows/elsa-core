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

        builder.Root = new Flowchart
        {
            Start = stageOne,
            Activities = { stageOne, stageTwo, done },
            Connections =
            {
                new Connection(new Endpoint(stageOne, "Done"), new Endpoint(stageTwo)),
                new Connection(new Endpoint(stageTwo, "Done"), new Endpoint(done))
            }
        };
    }

    protected static HttpEndpoint CreateEndpoint(string path, bool authorize, string? policy = null) => new()
    {
        Path = new(path),
        SupportedMethods = new([HttpMethods.Post]),
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
