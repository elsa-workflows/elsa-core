using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.CommandExecuter.Utils;
public static class TypeUtils
{
    public static Type GetType(string typeName)
    {
        return AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetType(typeName)).FirstOrDefault(x => x != null);
    }
}
