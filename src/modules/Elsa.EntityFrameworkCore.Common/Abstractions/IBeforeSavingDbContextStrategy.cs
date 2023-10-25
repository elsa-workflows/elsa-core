using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Elsa.EntityFrameworkCore.Common.Abstractions;

public interface IBeforeSavingDbContextStrategy : IDbContextStrategy
{
    Task<bool> CanExecute(EntityEntry entityEntry);

    Task Execute(EntityEntry entityEntry);
}
