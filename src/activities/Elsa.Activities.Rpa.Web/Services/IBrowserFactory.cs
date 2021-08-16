using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;

namespace Elsa.Activities.Rpa.Web.Services
{
    public interface IBrowserFactory
    {
        Task<string> OpenAsync(string driverType, object? options = default, CancellationToken cancellationToken = default);
        IWebDriver GetDriver(string driverId);
        Task CloseBrowserAsync(string? driverId);
    }
}