using System;
using System.Linq.Expressions;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.EntityFramework.Core.Services;
using Elsa.Persistence.Specifications;
using Microsoft.Extensions.Logging;

namespace Elsa.Persistence.EntityFramework.Core.Stores;

public class EntityFrameworkTriggerStore : ElsaContextEntityFrameworkStore<Trigger>, ITriggerStore
{
    public EntityFrameworkTriggerStore(IElsaContextFactory dbContextFactory, IMapper mapper, ILogger<EntityFrameworkTriggerStore> logger) : base(dbContextFactory, mapper, logger)
    {
    }

    protected override Expression<Func<Trigger, bool>> MapSpecification(ISpecification<Trigger> specification)
    {
        return AutoMapSpecification(specification);
    }
}