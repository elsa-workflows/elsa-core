using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.EntityFramework.Core.Services;
using Microsoft.Extensions.Logging;

namespace Elsa.Persistence.EntityFramework.Core.Stores
{
    public abstract class ElsaContextEntityFrameworkStore<T> : EntityFrameworkStore<T, ElsaContext> where T : class, IEntity
    {
        protected ElsaContextEntityFrameworkStore(IContextFactory<ElsaContext> dbContextFactory, IMapper mapper, ILogger logger) : base(dbContextFactory, mapper, logger)
        {
        }
    }
}