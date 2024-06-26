using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Elsa.Activities.Rpa.Web.DriverTypes.Chrome;
using Newtonsoft.Json;

namespace Elsa.Activities.Rpa.Web.Services
{
    public class ChromeDriverInstaller
    {
        private static readonly HttpClient HttpClient = new()
        {
            BaseAddress = new Uri("https://chromedriver.storage.googleapis.com/")
        };
        
        public Task Install() => Install(null, false);
        public Task Install(string chromeVersion) => Install(chromeVersion, false);
        public Task Install(bool forceDownload) => Install(null, forceDownload);
        
        private async Task Install(string? chromeVersion, bool forceDownload)
        {
            // Instructions from https://chromedriver.chromium.org/downloads/version-selection
            //   First, find out which version of Chrome you are using. Let's say you have Chrome 72.0.3626.81.
            
            chromeVersion ??= await GetChromeVersion();
            
            //   Take the Chrome version number, remove the build number 
            var chromeVersionWithoutBuiltNumber = chromeVersion[..chromeVersion.LastIndexOf('.')];
            
            var chromeDriverVersion = chromeVersion;
            
            var chromeVersionForTesting = new ChromeVersion();
            
            // After 115 chromedriver changed the way it was downloaded so we need to account this new functionality https://developer.chrome.com/docs/chromedriver/downloads/version-selection
            var useChromeForTestingDownloadApi = int.TryParse(chromeVersion.Split(".")[0], out var chromeMainVersionNumber) && chromeMainVersionNumber >= 115;
            
            string zipName;
            var driverName = "chromedriver";
            string platformName;
            
            var folderName = string.Empty;
            
            if (useChromeForTestingDownloadApi)
            {
                var chromeToTestResponse = await HttpClient.GetAsync($"https://googlechromelabs.github.io/chrome-for-testing/known-good-versions-with-downloads.json");
                var jsonResult = await chromeToTestResponse.Content.ReadAsStringAsync();
                var chromeForTestingResult = JsonConvert.DeserializeObject<ChromeForTesting>(jsonResult);
                chromeVersionForTesting = chromeForTestingResult?.Versions.SingleOrDefault(x => x.VersionNumber == chromeVersion)
                                          ?? chromeForTestingResult!.Versions.Last(x => x.VersionNumber.Contains(chromeVersionWithoutBuiltNumber));
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    folderName = RuntimeInformation.ProcessArchitecture == Architecture.X64 ? "chromedriver-win64" : "chromedriver-win32";
                    zipName = Path.ChangeExtension(folderName, "zip");
                    driverName = Path.ChangeExtension(driverName, "exe");
                    platformName = RuntimeInformation.ProcessArchitecture == Architecture.X64 ? "win64" : "win32";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    folderName = "chromedriver-linux64";
                    zipName = Path.ChangeExtension(folderName, "zip");
                    platformName = "linux64";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    folderName = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "chromedriver-mac-arm64" : "chromedriver-mac-x64";
                    zipName = Path.ChangeExtension(folderName, "zip");
                    platformName = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "mac-arm64" : "mac-x64";
                }
                else
                {
                    throw new PlatformNotSupportedException("Your operating system is not supported.");
                }
            }
            else
            {
                //   and append the result to URL "https://chromedriver.storage.googleapis.com/LATEST_RELEASE_". 
                //   For example, with Chrome version 72.0.3626.81, you'd get a URL "https://chromedriver.storage.googleapis.com/LATEST_RELEASE_72.0.3626".
                var chromeDriverVersionResponse = await HttpClient.GetAsync($"LATEST_RELEASE_{chromeVersion}");
                if (!chromeDriverVersionResponse.IsSuccessStatusCode)
                {
                    if (chromeDriverVersionResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new Exception($"ChromeDriver version not found for Chrome version {chromeVersion}");
                    }
                    
                    throw new Exception($"ChromeDriver version request failed with status code: {chromeDriverVersionResponse.StatusCode}, reason phrase: {chromeDriverVersionResponse.ReasonPhrase}");
                }
                
                chromeDriverVersion = await chromeDriverVersionResponse.Content.ReadAsStringAsync();
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    zipName = "chromedriver_win32.zip";
                    driverName = "chromedriver.exe";
                    platformName = "win32";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    zipName = "chromedriver_linux64.zip";
                    driverName = "chromedriver";
                    platformName = "linux64";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    zipName = "chromedriver_mac64.zip";
                    driverName = "chromedriver";
                    platformName = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "mac-arm64" : "mac-x64";
                }
                else
                {
                    throw new PlatformNotSupportedException("Your operating system is not supported.");
                }
            }
            
            var targetPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            targetPath = Path.Combine(targetPath, driverName);
            if (!forceDownload && File.Exists(targetPath))
            {
                using var process = Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = targetPath,
                        ArgumentList = { "--version" },
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    }
                )!;
                var existingChromeDriverVersion = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();
                process.Kill();
                
                // expected output is something like "ChromeDriver 88.0.4324.96 (68dba2d8a0b149a1d3afac56fa74648032bcf46b-refs/branch-heads/4324@{#1784})"
                // the following line will extract the version number and leave the rest
                existingChromeDriverVersion = existingChromeDriverVersion.Split(" ")[1];
                if (chromeDriverVersion == existingChromeDriverVersion)
                {
                    return;
                }
                
                if (!string.IsNullOrEmpty(error))
                {
                    throw new Exception($"Failed to execute {driverName} --version");
                }
            }
            
            //   Use the URL created in the last step to retrieve a small file containing the version of ChromeDriver to use. For example, the above URL will get your a file containing "72.0.3626.69". (The actual number may change in the future, of course.)
            //   Use the version number retrieved from the previous step to construct the URL to download ChromeDriver. With version 72.0.3626.69, the URL would be "https://chromedriver.storage.googleapis.com/index.html?path=72.0.3626.69/".
            var driverZipResponse = useChromeForTestingDownloadApi
                ? await HttpClient.GetAsync(chromeVersionForTesting.Downloads.Chromedriver.Single(x => x.Platform == platformName).Url)
                : await HttpClient.GetAsync($"{chromeDriverVersion}/{zipName}");
            if (!driverZipResponse.IsSuccessStatusCode)
            {
                throw new Exception($"ChromeDriver download request failed with status code: {driverZipResponse.StatusCode}, reason phrase: {driverZipResponse.ReasonPhrase}");
            }
            
            // this reads the zipfile as a stream, opens the archive, 
            // and extracts the chromedriver executable to the targetPath without saving any intermediate files to disk
            using (var zipFileStream = await driverZipResponse.Content.ReadAsStreamAsync())
            using (var zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Read))
            using (var chromeDriverWriter = new FileStream(targetPath, FileMode.Create))
            {
                var entry = zipArchive.GetEntry($"{folderName}/{driverName}")!;
                using Stream chromeDriverStream = entry.Open();
                await chromeDriverStream.CopyToAsync(chromeDriverWriter);
            }
            
            // on Linux/macOS, you need to add the executable permission (+x) to allow the execution of the chromedriver
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                using var process = Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "chmod",
                        ArgumentList = { "+x", targetPath },
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    }
                )!;
                string error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();
                process.Kill();
                
                if (!string.IsNullOrEmpty(error))
                {
                    throw new Exception("Failed to make chromedriver executable");
                }
            }
        }
        
        public async Task<string> GetChromeVersion()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var chromePath = (string?)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\chrome.exe", null, null);
                if (chromePath == null)
                {
                    throw new Exception("Google Chrome not found in registry");
                }
                
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(chromePath);
                return fileVersionInfo.FileVersion;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                try
                {
                    using var process = Process.Start(
                        new ProcessStartInfo
                        {
                            FileName = "google-chrome",
                            ArgumentList = { "--product-version" },
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                        }
                    )!;
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    process.WaitForExit();
                    process.Kill();
                    
                    if (!string.IsNullOrEmpty(error))
                    {
                        throw new Exception(error);
                    }
                    
                    return output;
                }
                catch (Exception ex)
                {
                    throw new Exception("An error occurred trying to execute 'google-chrome --product-version'", ex);
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    using var process = Process.Start(
                        new ProcessStartInfo
                        {
                            FileName = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
                            ArgumentList = { "--version" },
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                        }
                    )!;
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    process.WaitForExit();
                    process.Kill();
                    
                    if (!string.IsNullOrEmpty(error))
                    {
                        throw new Exception(error);
                    }
                    
                    output = output.Replace("Google Chrome ", "");
                    return output;
                }
                catch (Exception ex)
                {
                    throw new Exception($"An error occurred trying to execute '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome --version'", ex);
                }
            }
            else
            {
                throw new PlatformNotSupportedException("Your operating system is not supported.");
            }
        }
    }
}