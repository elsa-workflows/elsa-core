using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Persistence.EntityFrameworkCore.DbContexts
{
    public interface IDbContextCustomSchema
    {
        bool UseCustomSchema { get; }
        string CustomDefaultSchema { get; set; }
        string CustomMigrationsHistoryTableName { get; set; }
    }
}