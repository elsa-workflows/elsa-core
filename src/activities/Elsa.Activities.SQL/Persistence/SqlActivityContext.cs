using Elsa.Models;
using Elsa.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Activities.SQL.Persistence
{
    public class SqlActivity : Entity
    {

    }

    public class SqlActivityContext : ElsaContext 
    {
        public SqlActivityContext(DbContextOptions options) : base(options)
        {
        }
    }
}
