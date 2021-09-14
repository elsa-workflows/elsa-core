using Elsa.Activities.Rpa.Web.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.Rpa.Web.Services
{
    public class BrowserFactory : IBrowserFactory
    {
        private readonly RpaWebOptions _options;
        private readonly ILogger<BrowserFactory> _logger;

        public BrowserFactory(
            IOptions<RpaWebOptions> options,
            ILogger<BrowserFactory> logger
        )
        {
            _options = options.Value;
            _logger = logger;
        }
        private Dictionary<string, IWebDriver> _drivers { get; set; } = new Dictionary<string, IWebDriver>();
        
        /// <returns>the driverId</returns>
        public async Task<string> OpenAsync(string driverType, object? options = default, CancellationToken cancellationToken=default)
        {
            var id = Guid.NewGuid().ToString();
            IWebDriver driver;
            
            switch(driverType)
            {
                case DriverType.Chrome:
                    {
                        var chromeDriverInstaller = new ChromeDriverInstaller();
                        var chromeVersion = await chromeDriverInstaller.GetChromeVersion();
                        await chromeDriverInstaller.Install(chromeVersion);
                        //chromeOptions.AddArguments("headless");
                        driver = new ChromeDriver(options as ChromeOptions);
                        break;
                    }
                case DriverType.Firefox:
                    {
                        throw new NotImplementedException();
                    }
                case DriverType.InternetExplorer:
                    {
                        throw new NotImplementedException();
                    }
                case DriverType.Opera:
                    {
                        throw new NotImplementedException();
                    }
                default: { throw new Exception($"Invalid driver type {driverType}"); }
            }
                      
            _drivers[id]=driver;
            return id;
        }
        public IWebDriver GetDriver(string driverId)
        {
            return _drivers[driverId];
        }

        public Task CloseBrowserAsync(string? driverId)
        {
            GetDriver(driverId!).Dispose();
            _drivers.Remove(driverId!);
            return Task.CompletedTask;
        }
    }
}