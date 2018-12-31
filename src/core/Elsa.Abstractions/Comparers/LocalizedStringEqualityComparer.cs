using System.Collections.Generic;
using Microsoft.Extensions.Localization;

namespace Elsa.Comparers
{
    public class LocalizedStringEqualityComparer : IEqualityComparer<LocalizedString>
    {
        public bool Equals(LocalizedString x, LocalizedString y)
        {
            return x.Name.Equals(y.Name);
        }

        public int GetHashCode(LocalizedString obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}