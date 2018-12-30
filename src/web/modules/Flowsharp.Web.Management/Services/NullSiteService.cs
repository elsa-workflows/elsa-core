using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using OrchardCore.Settings;

namespace Flowsharp.Web.Management.Services
{
    public class NullSiteService : ISiteService
    {
        public Task<ISite> GetSiteSettingsAsync()
        {
            var settings = new SiteSettings
            {
                Properties =
                {
                    ["CurrentThemeName"] = "TheAdminatorTheme"
                }
            };
            return Task.FromResult<ISite>(settings);
        }

        public Task UpdateSiteSettingsAsync(ISite site)
        {
            return Task.CompletedTask;
        }

        public IChangeToken ChangeToken => new CancellationChangeToken(CancellationToken.None);
    }
}