using System;
using System.Threading;
using Elsa.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Models;

public record HttpEndpointFaultedWorkflowContext(HttpContext HttpContext, WorkflowInstance WorkflowInstance, Exception? Exception, CancellationToken CancellationToken);