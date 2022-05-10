using OpenQA.Selenium;
using WebDriverManager;
using OpenQA.Selenium.Firefox;
using System.Diagnostics;
using WebDriverManager.DriverConfigs.Impl;

using _s = LikeAllStrava.ShraredObjects;

namespace LikeAllStrava
{
    public class FirefoxDriverControl
    {
        public static void InitializeFirefoxDriver()
        {
            // Download updated Firefox driver on the fly
            new DriverManager().SetUpDriver(new FirefoxConfig());

            FirefoxDriverService service = FirefoxDriverService.CreateDefaultService();
            service.SuppressInitialDiagnosticInformation = true;  // Disable logs
            service.HideCommandPromptWindow = true;               // Hide window

            _s.FirefoxDriver = new FirefoxDriver(service);
            _s.JavascriptExecutor = (IJavaScriptExecutor)_s.FirefoxDriver;
        }

        public static void CloseAllGeckoDrivers()
        {
            // Close all geckodriver processes open
            Console.WriteLine("Closing all geckodriver processes open...");
            Process process = Process.Start("taskkill", "/F /IM geckodriver.exe /T");
            process.WaitForExit();
            Console.WriteLine("DONE!");
        }
    }
}
