using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Server.Authentication.Contexts
{
    public static class ElsaAuthenticationContext
    {
        public static List<AuthenticationStyles> AuthenticationStyles { get; internal set; } = new List<AuthenticationStyles>();
        public static string CurrentTenantAccessorName { get; internal set; }
        public static string TenantAccessorKeyName { get; set; }
    }
}
