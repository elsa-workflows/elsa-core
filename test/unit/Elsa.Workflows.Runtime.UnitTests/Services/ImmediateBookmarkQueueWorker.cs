using Elsa.Common.Multitenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

internal class ImmediateBookmarkQueueWorker(
    IBookmarkQueueSignaler signaler,
    IServiceScopeFactory scopeFactory,
    ILogger<BookmarkQueueWorker> logger,
    ITenantScopeFactory? tenantScopeFactory = null,
    ITenantAccessor? tenantAccessor = null) : BookmarkQueueWorker(signaler, scopeFactory, logger, tenantScopeFactory, tenantAccessor, TimeSpan.Zero);
