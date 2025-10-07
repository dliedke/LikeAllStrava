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
            Thread.Sleep(5000); // Increased from 3 to 5 seconds

            var options = new EdgeOptions
            {
                DebuggerAddress = "127.0.0.1:59492"
            };

            // Retry logic in case Edge isn't ready
            EdgeDriver? driver = null;
            int retries = 3;
            Exception? lastException = null;

            for (int i = 0; i < retries; i++)
            {
                try
                {
                    driver = new EdgeDriver(options);
                    break; // Success, exit retry loop
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Console.WriteLine($"Attempt {i + 1}/{retries} failed to connect to Edge: {ex.Message}");
                    if (i < retries - 1)
                    {
                        Thread.Sleep(2000); // Wait before retry
                    }
                }
            }

            if (driver == null)
            {
                throw new Exception("Failed to initialize Edge driver after " + retries + " attempts: " + lastException?.Message);
            }

            try
            {
                _s.EdgeDriver = driver;
                _s.JavascriptExecutor = (IJavaScriptExecutor)_s.EdgeDriver;

                // Give browser a moment to stabilize after connection
                Thread.Sleep(2000); // Increased from 1 to 2 seconds

                // Verify browser is still responsive
                string currentUrl = "";
                try
                {
                    currentUrl = driver.Url;
                    Console.WriteLine($"Browser connected. Current URL: {currentUrl}");
                }
                catch (Exception ex)
                {
                    throw new Exception("Browser disconnected immediately after connection: " + ex.Message);
                }

                // Switch to the last window handle (usually the main window)
                var windowHandles = driver.WindowHandles;
                if (windowHandles.Count > 0)
                {
                    driver.SwitchTo().Window(windowHandles[windowHandles.Count - 1]);

                    // Give window switch time to complete
                    Thread.Sleep(1000); // Increased from 500ms to 1s

                    // Open a new tab if needed - handle internal Edge URLs
                    string url = driver.Url;
                    if (url.StartsWith("chrome-extension") || url.StartsWith("edge://"))
                    {
                        Console.WriteLine($"Internal Edge page detected ({url}), navigating to blank page...");

                        // Navigate directly instead of opening new tab (safer for internal pages)
                        driver.Navigate().GoToUrl("about:blank");
                        Thread.Sleep(1000); // Wait for navigation
                    }

                    Console.WriteLine($"Active tab URL: {driver.Url}");
                }
                else
                {
                    throw new Exception("No window handles available - browser may have closed");
                }
            }
            catch (Exception ex)
            {
                // Clean up the driver if initialization failed
                try
                {
                    driver?.Quit();
                }
                catch { }

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
