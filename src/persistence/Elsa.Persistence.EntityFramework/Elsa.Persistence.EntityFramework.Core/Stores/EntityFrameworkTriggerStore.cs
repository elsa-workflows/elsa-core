using System;
using System.Linq.Expressions;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.EntityFramework.Core.Services;
using Elsa.Persistence.Specifications;

namespace Elsa.Persistence.EntityFramework.Core.Stores;

public class EntityFrameworkTriggerStore : ElsaContextEntityFrameworkStore<Trigger>, ITriggerStore
{
    public EntityFrameworkTriggerStore(Func<IElsaContextFactory> getDbContextFactory, IMapper mapper) : base(getDbContextFactory, mapper)
    {
    }

    protected override Expression<Func<Trigger, bool>> MapSpecification(ISpecification<Trigger> specification)
    {
        return AutoMapSpecification(specification);
    }
}