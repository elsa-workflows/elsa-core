using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;

namespace Elsa.Activities.Rpa.Web.Services
{
    public interface IBrowserFactory
    {
        Task<string> OpenAsync(CancellationToken cancellationToken);
        IWebDriver GetDriver(string driverId);
        Task CloseBrowserAsync(string? driverId);
    }
}