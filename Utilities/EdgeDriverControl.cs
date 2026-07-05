using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using _s = LikeAllStrava.SharedObjects;

namespace LikeAllStrava
{
    public class EdgeDriverControl
    {
        public static void InitializeEdgeDriver()
        {
            // Recent Edge/Chromium builds refuse to open the DevTools remote debugging
            // port when the resolved user data directory is the real default profile
            // ("DevTools remote debugging requires a non-default data directory") -
            // even if --user-data-dir is passed explicitly pointing at that same path.
            // So we use a genuinely separate, dedicated profile directory instead, and
            // seed it once from the user's real profile so the existing Strava login
            // (cookies/local storage) carries over instead of requiring a fresh login.
            string realUserDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "Edge", "User Data");
            string automationUserDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LikeAllStrava", "EdgeProfile");

            if (!Directory.Exists(automationUserDataDir))
            {
                SeedAutomationProfile(realUserDataDir, automationUserDataDir);
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                // Launch with about:blank to avoid edge://discover-chat-v2/ and extension conflicts
                Arguments = $"--remote-debugging-port=59492 --start-maximized --user-data-dir=\"{automationUserDataDir}\" --profile-directory=Default about:blank"
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

        // Seeds a fresh automation profile from the user's real Edge profile so the
        // existing Strava login carries over instead of requiring a manual re-login.
        // Skips cache/heavy folders that aren't needed for an authenticated session.
        private static void SeedAutomationProfile(string realUserDataDir, string automationUserDataDir)
        {
            string[] skipFolderNames = { "Cache", "Code Cache", "GPUCache", "GrShaderCache", "ShaderCache", "System Profile", "CrashpadMetrics" };

            try
            {
                Console.WriteLine("First run: creating dedicated automation profile and copying existing Strava login...");
                Directory.CreateDirectory(automationUserDataDir);

                string realLocalState = Path.Combine(realUserDataDir, "Local State");
                if (File.Exists(realLocalState))
                {
                    File.Copy(realLocalState, Path.Combine(automationUserDataDir, "Local State"), true);
                }

                string realDefaultProfile = Path.Combine(realUserDataDir, "Default");
                string automationDefaultProfile = Path.Combine(automationUserDataDir, "Default");
                if (Directory.Exists(realDefaultProfile))
                {
                    CopyDirectory(realDefaultProfile, automationDefaultProfile, skipFolderNames);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Warning: failed to seed automation profile from existing Edge profile: " + ex.Message);
                Console.WriteLine("You may need to log in to Strava manually the first time in the automated browser window.");
            }
        }

        private static void CopyDirectory(string sourceDir, string destDir, string[] skipFolderNames)
        {
            Directory.CreateDirectory(destDir);

            foreach (string filePath in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(filePath));
                try
                {
                    File.Copy(filePath, destFile, true);
                }
                catch
                {
                    // Skip files locked by a running Edge process or otherwise inaccessible
                }
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string folderName = Path.GetFileName(subDir);
                if (skipFolderNames.Contains(folderName, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }
                CopyDirectory(subDir, Path.Combine(destDir, folderName), skipFolderNames);
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
