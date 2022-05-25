using System;
using System.Threading;
using Elsa.Workflows.Persistence.Entities;
using Microsoft.AspNetCore.Http;

namespace Elsa.Http.Models;

public record HttpEndpointFaultedWorkflowContext(HttpContext HttpContext, WorkflowInstance WorkflowInstance, Exception? Exception, CancellationToken CancellationToken);