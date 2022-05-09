using OpenQA.Selenium;
using System.Diagnostics;
using OpenQA.Selenium.Chrome;

using _s = LikeAllStrava.ShraredObjects;

namespace LikeAllStrava
{
    public class ChromeDriverControl
    {
        public static void InitializeChromeDriver()
        {
            // Initialize ChromeDriver with no logs at all
            string currentRuntimeDirectory = AppContext.BaseDirectory;

            ChromeDriverService service = ChromeDriverService.CreateDefaultService(currentRuntimeDirectory);
            service.SuppressInitialDiagnosticInformation = true;  // Disable logs
            service.EnableVerboseLogging = false;                 // Disable logs
            service.EnableAppendLog = false;                      // Disable logs
            service.HideCommandPromptWindow = true;               // Hide window

            ChromeOptions options = new();
            options.AddArgument("start-maximized");               // Start Chrome window maximized
            options.AddArgument("--no-sandbox");                  // Use same profile as the user
            options.AddArgument("--disable-gpu");                 // Disable hardware acceleration because it shows washed out Strava sometimes in HDR screens

            _s.ChromeDriver = new ChromeDriver(service, options);
            _s.JavascriptExecutor = (IJavaScriptExecutor)_s.ChromeDriver;
        }

        public static void CloseAllChromeDrivers()
        {
            // Close all ChromeDriver processes open
            Console.WriteLine("Closing all ChromeDriver processes open...");
            Process process = Process.Start("taskkill", "/F /IM chromedriver.exe /T");
            process.WaitForExit();
            Console.WriteLine("DONE!");
        }
    }
}
