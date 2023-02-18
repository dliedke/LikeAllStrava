using OpenQA.Selenium;
using WebDriverManager;
using OpenQA.Selenium.Chrome;
using System.Diagnostics;
using WebDriverManager.DriverConfigs.Impl;

using _s = LikeAllStrava.ShraredObjects;

namespace LikeAllStrava
{
    public class ChromeDriverControl
    {
        public static void InitializeChromeDriver()
        {
            // Download updated Chrome driver on the fly
            new DriverManager().SetUpDriver(new ChromeConfig());

            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
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
            Process process = Process.Start("taskkill", "/F /IM ChromeDriver.exe /T");
            process.WaitForExit();

            // Delete scoped_dir directories from temp created by ChromeDriver
            string tempDir = Environment.ExpandEnvironmentVariables("%temp%");
            string[] scopedDirs = Directory.GetDirectories(tempDir, "scoped_dir*");
            foreach (string path in scopedDirs) 
            {
                DirectoryInfo di = new(path);
                if (di.Exists)
                {
                    di.Delete(true);
                }
            }

            Console.WriteLine("DONE!");
        }
    }
}
