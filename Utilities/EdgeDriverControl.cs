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
            // Start Edge maximized using Edge debugger
            Process.Start(new ProcessStartInfo
            {
                FileName = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                Arguments = "--remote-debugging-port=58492 --start-maximized"
            });

            // Wait 3 seconds for Edge to fully initialize
            Thread.Sleep(3000);

            // Attach to existing Edge instance
            var options = new EdgeOptions
            {
                DebuggerAddress = "127.0.0.1:58492"
            };
            try
            {
                var driver = new EdgeDriver(options);
                _s.EdgeDriver = driver;
                _s.JavascriptExecutor = (IJavaScriptExecutor)_s.EdgeDriver;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to initialize Edge driver: " + ex.Message);
            }
        }

        public static void CloseAllEdgeDrivers()
        {
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
