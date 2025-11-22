using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
                // Launch with about:blank to avoid edge://discover-chat-v2/ and extension conflicts
                Arguments = "--remote-debugging-port=59492 --start-maximized about:blank"
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

                // Verify browser is still responsive and check for problematic pages
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

                // If on a problematic internal page, create a new tab and close the old one
                if (currentUrl.StartsWith("edge://") || currentUrl.StartsWith("chrome-extension://"))
                {
                    Console.WriteLine($"Detected problematic page ({currentUrl}), creating new clean tab...");

                    var originalHandles = driver.WindowHandles;

                    // Create a new window/tab using CDP (Chrome DevTools Protocol) which bypasses the extension
                    try
                    {
                        driver.ExecuteCdpCommand("Target.createTarget", new Dictionary<string, object>
                        {
                            { "url", "about:blank" }
                        });
                        Thread.Sleep(1500);

                        // Switch to the new window
                        var newHandles = driver.WindowHandles;
                        string? newHandle = newHandles.FirstOrDefault(h => !originalHandles.Contains(h));
                        if (newHandle != null)
                        {
                            driver.SwitchTo().Window(newHandle);
                            Thread.Sleep(500);
                            Console.WriteLine("Successfully switched to new clean tab");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Failed to create new tab via CDP: {ex.Message}");
                    }
                }
                else
                {
                    // Not on a problematic page, just ensure we're on the right window
                    var windowHandles = driver.WindowHandles;
                    if (windowHandles.Count > 0)
                    {
                        driver.SwitchTo().Window(windowHandles[windowHandles.Count - 1]);
                        Thread.Sleep(1000);
                    }
                }

                Console.WriteLine($"Active tab URL: {driver.Url}");
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
