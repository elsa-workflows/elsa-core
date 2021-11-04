using System;
using System.Linq.Expressions;
using Elsa.Persistence.EntityFramework.Core.Stores;
using Elsa.Persistence.Specifications;
using Elsa.Serialization;
using Elsa.WorkflowSettings.Models;
using AutoMapper;
using Elsa.WorkflowSettings.Persistence.EntityFramework.Core.Services;
using Elsa.Persistence.EntityFramework.Core;

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

        //protected override void OnSaving(ElsaContext dbContext, WorkflowSetting entity)
        //{
        //    var data = new
        //    {
        //        entity.DefaultValue,
        //        entity.Connections,
        //        entity.Variables,
        //        entity.ContextOptions,
        //        entity.CustomAttributes,
        //        entity.Channel
        //    };

        //    var json = _contentSerializer.Serialize(data);
        //    dbContext.Entry(entity).Property("Data").CurrentValue = json;
        //}
    }
}