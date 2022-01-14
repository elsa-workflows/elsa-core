using System;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.EntityFramework.Core.Services;

namespace Elsa.Persistence.EntityFramework.Core.Stores
{
    public abstract class ElsaContextEntityFrameworkStore<T> : EntityFrameworkStore<T, ElsaContext> where T : class, IEntity
    {
        protected ElsaContextEntityFrameworkStore(Func<IContextFactory<ElsaContext>> getDbContextFactory, IMapper mapper) : base(getDbContextFactory, mapper)
        {
        }
    }
}