using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Expressions.Contracts;
using Elsa.Workflows.Memory;

namespace Elsa.Workflows.Core.UnitTests;
public class WellKnownTypes : IWellKnownTypeRegistry
{
    public IEnumerable<Type> ListTypes()
    {
        throw new NotImplementedException();
    }

    public void RegisterType(Type type, string alias)
    {
        throw new NotImplementedException();
    }

    public bool TryGetAlias(Type type, out string alias)
    {
        throw new NotImplementedException();
    }

    public bool TryGetType(string alias, out Type type)
    {
        if (alias == "Variable")
        {
            type = typeof(Variable);
            return true;
        }
        else
        {
            type = typeof(object);
            return false;
        }

    }
}
