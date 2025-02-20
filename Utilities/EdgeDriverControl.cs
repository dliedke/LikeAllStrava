using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using System.Diagnostics;

using _s = LikeAllStrava.ShraredObjects;

namespace LikeAllStrava
{
    public class EdgeDriverControl
    {
        public static void InitializeEdgeDriver()
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                Arguments = "--remote-debugging-port=59492 --start-maximized"
            };

            Process.Start(processInfo);
            Thread.Sleep(3000);

            var options = new EdgeOptions
            {
                DebuggerAddress = "127.0.0.1:59492"
            };

            try
            {
                var driver = new EdgeDriver(options);
                _s.EdgeDriver = driver;
                _s.JavascriptExecutor = (IJavaScriptExecutor)_s.EdgeDriver;

                // Switch to the last window handle (usually the main window)
                var windowHandles = driver.WindowHandles;
                driver.SwitchTo().Window(windowHandles[windowHandles.Count - 1]);

                // Open a new tab if needed
                if (driver.Url.StartsWith("chrome-extension"))
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript("window.open()");
                    windowHandles = driver.WindowHandles;
                    driver.SwitchTo().Window(windowHandles[windowHandles.Count - 1]);
                }

                Console.WriteLine($"Active tab URL: {driver.Url}");
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to initialize Edge driver: " + ex.Message);
            }
        }

        public static void CloseAllEdgeDrivers()
        {
            if (_s.EdgeDriver != null)
            {
                _s.EdgeDriver?.Quit();
            }

            // Close all EdgeDriver processes open
            Console.WriteLine("Closing all EdgeDriver processes open...");
            Process process = Process.Start("taskkill", "/F /IM msedgedriver.exe /T");
            process.WaitForExit();
            System.Threading.Thread.Sleep(2000);

            // Delete scoped_dir directories from temp created by EdgeDriver
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

        // Create method to close all Edge browser windows using process
        public static void CloseAllEdgeBrowserWindows()
        {
            // Close all Edge browser windows
            Console.WriteLine("Closing all Edge browser windows...");
            Process process = Process.Start("taskkill", "/F /IM msedge.exe /T");
            process.WaitForExit();
            System.Threading.Thread.Sleep(2000);
            Console.WriteLine("DONE!");
        }
    }
}
