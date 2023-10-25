using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Elsa.EntityFrameworkCore.Common.Abstractions;

public interface IModelCreatingDbContextStrategy : IDbContextStrategy
{
    bool CanExecute(ModelBuilder modelBuilder, IMutableEntityType mutableEntity);

    void Execute(ModelBuilder modelBuilder, IMutableEntityType mutableEntity);
}
