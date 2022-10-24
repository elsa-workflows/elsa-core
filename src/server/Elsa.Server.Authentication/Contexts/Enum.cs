using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Server.Authentication.Contexts
{
    public enum AuthenticationStyles
    {
        OpenIdConnect = 1,
        ServerManagedCookie ,
        JwtBearerToken,

    }


}
