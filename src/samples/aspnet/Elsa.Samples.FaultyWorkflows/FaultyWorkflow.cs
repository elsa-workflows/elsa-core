using System;
using System.Net;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Http;
using Elsa.Activities.Http.Models;
using Elsa.Builders;
using Elsa.Serialization;
using Elsa.Services.Models;

namespace Elsa.Samples.FaultyWorkflows
{
    /// <summary>
    /// A workflow that is triggered when HTTP requests are made to /hello and writes a response.
    /// </summary>
    public class FaultyWorkflow : IWorkflow
    {
        private readonly IContentSerializer _contentSerializer;

        public FaultyWorkflow(IContentSerializer contentSerializer)
        {
            _contentSerializer = contentSerializer;
        }
        
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .HttpRequestReceived("/faulty")
                .IfTrue(context => context.GetInput<HttpRequestModel>()!.QueryString.GetItem("fault")?.Value == "true", ifTrue =>
                    ifTrue.Then(() => throw new Exception("This is quite a serious fault!")))
                .WriteHttpResponse(response => response.WithStatusCode(HttpStatusCode.OK).WithContentType("application/json").WithContent(WriteWorkflowInfoAsync));
        }

        private string WriteWorkflowInfoAsync(ActivityExecutionContext context)
        {
            var model = new
            {
                WorkflowInstanceId = context.WorkflowInstance.Id,
                WorkflowStatus = context.WorkflowInstance.WorkflowStatus
            };

            return _contentSerializer.Serialize(model);
        }
    }
}