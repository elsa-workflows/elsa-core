using Microsoft.EntityFrameworkCore;

namespace Elsa.WorkflowSettings.Persistence.EntityFramework.Core.Services
{
    public class WorkflowSettingsContextFactory<TWorkflowSettingsContext> : IWorkflowSettingsContextFactory where TWorkflowSettingsContext : WorkflowSettingsContext
    {
        private readonly IDbContextFactory<TWorkflowSettingsContext> _contextFactory;
        public WorkflowSettingsContextFactory(IDbContextFactory<TWorkflowSettingsContext> contextFactory) => _contextFactory = contextFactory;
        public WorkflowSettingsContext CreateDbContext() => _contextFactory.CreateDbContext();
    }
}