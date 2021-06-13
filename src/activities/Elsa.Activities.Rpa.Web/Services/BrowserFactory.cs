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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>the DriverId</returns>
        public async Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid().ToString();
            var chromeDriverInstaller = new ChromeDriverInstaller();
            var chromeVersion = await chromeDriverInstaller.GetChromeVersion();
            await chromeDriverInstaller.Install(chromeVersion);
            var chromeOptions = new ChromeOptions();
            //chromeOptions.AddArguments("headless");
            var driver = new ChromeDriver(chromeOptions);            
            _drivers[id]=driver;
            return id;
        }
        public IWebDriver GetDriver(string driverId)
        {
            return _drivers[driverId];
        }

        public async Task CloseBrowserAsync(string? driverId)
        {
            GetDriver(driverId).Dispose();
            _drivers.Remove(driverId);
        }
    }
}