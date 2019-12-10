using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Persistence.EntityFrameworkCore.DbContexts
{
    public interface IDbContextCustomSchema
    {
        bool UseCustomSchema { get; }
        string Schema { get; set; }
        string MigrationsHistoryTableName { get; set; }
    }
}