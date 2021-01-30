using System;
using System.Net;
using Elsa.Activities.Http;
using Elsa.Activities.Http.Models;
using Elsa.Builders;
using Elsa.Serialization;
using Elsa.Services.Models;

namespace Elsa.Samples.Server.Host.Workflows
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
                .Then(MaybeThrow)
                .WriteHttpResponse(response => response.WithStatusCode(HttpStatusCode.OK).WithContentType("application/json").WithContent(WriteWorkflowInfoAsync));
        }

        private void MaybeThrow(ActivityExecutionContext context)
        {
            var model = context.GetInput<HttpRequestModel>()!;
            var fault = model.QueryString.GetItem("fault")?.Value == "true";

            if (fault)
                throw new Exception("This is quite a serious fault!");
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