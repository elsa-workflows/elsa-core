using Elsa.Common.Multitenancy;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.ComponentTests.Services;

/// <summary>
/// A test-specific bookmark queue worker that processes items immediately without throttling.
/// This prevents timeouts in tests where many workflows complete rapidly.
/// </summary>
public class TestBookmarkQueueWorker(
    IBookmarkQueueSignaler signaler,
    IServiceScopeFactory scopeFactory,
    ILogger<TestBookmarkQueueWorker> logger,
    ITenantScopeFactory? tenantScopeFactory = null,
    ITenantAccessor? tenantAccessor = null) : BookmarkQueueWorker(signaler, scopeFactory, logger, tenantScopeFactory, tenantAccessor, TimeSpan.Zero);
