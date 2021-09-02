using System;
using System.Linq.Expressions;
using Elsa.Persistence.EntityFramework.Core.Stores;
using Elsa.Persistence.Specifications;
using Elsa.Serialization;
using Elsa.WorkflowSettings.Models;
using AutoMapper;
using Elsa.WorkflowSettings.Persistence.EntityFramework.Core.Services;

namespace Elsa.WorkflowSettings.Persistence.EntityFramework.Core.Stores
{
    public class EntityFrameworkWorkflowSettingsStore : EntityFrameworkStore<WorkflowSetting, WorkflowSettingsContext>, IWorkflowSettingsStore
    {
        private readonly IContentSerializer _contentSerializer;

        public EntityFrameworkWorkflowSettingsStore(IWorkflowSettingsContextFactory dbContextFactory, IMapper mapper, IContentSerializer contentSerializer) : base(dbContextFactory, mapper)
        {
            _contentSerializer = contentSerializer;
        }

        protected override Expression<Func<WorkflowSetting, bool>> MapSpecification(ISpecification<WorkflowSetting> specification) => AutoMapSpecification(specification);
    }
}