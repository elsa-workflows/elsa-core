using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Persistence.EntityFrameworkCore.DbContexts
{
    public class OracleContext:ElsaContext
    {
        public OracleContext(DbContextOptions options) : base(options)
        {
        }
    }
}
