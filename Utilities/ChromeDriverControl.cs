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
            new DriverManager().SetUpDriver(new ChromeConfig());

            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.SuppressInitialDiagnosticInformation = true;
            service.EnableVerboseLogging = false;
            service.EnableAppendLog = false;
            service.HideCommandPromptWindow = true;

            ChromeOptions options = new();

            // Configurações básicas
            options.AddArgument("start-maximized");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-gpu");

            // Adiciona user agent realista
            options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            // Desativa automação WebDriver
            options.AddArgument("--disable-blink-features=AutomationControlled");

            // Adiciona algumas preferências para parecer mais humano
            options.AddUserProfilePreference("profile.default_content_setting_values.notifications", 2);
            options.AddUserProfilePreference("profile.password_manager_enabled", false);
            options.AddUserProfilePreference("credentials_enable_service", false);

            // Desativa PDF viewer
            options.AddUserProfilePreference("plugins.always_open_pdf_externally", true);

            // Adiciona extensões aleatórias comuns (opcional)
            // options.AddExtension("path_to_extension.crx");

            // Randomiza a resolução da janela
            var resolutions = new[] {
        (1920, 1080),
        (1366, 768),
        (1536, 864),
        (1440, 900)
    };
            var (width, height) = resolutions[new Random().Next(resolutions.Length)];
            options.AddArgument($"--window-size={width},{height}");

            _s.ChromeDriver = new ChromeDriver(service, options);
            _s.JavascriptExecutor = (IJavaScriptExecutor)_s.ChromeDriver;

            // Remove WebDriver flags usando JavaScript
            _s.JavascriptExecutor.ExecuteScript(@"
        Object.defineProperty(navigator, 'webdriver', {
            get: () => undefined
        });
    ");

        }

        public static void CloseAllChromeDrivers()
        {
            // Close all ChromeDriver processes open
            Console.WriteLine("Closing all ChromeDriver processes open...");
            Process process = Process.Start("taskkill", "/F /IM ChromeDriver.exe /T");
            process.WaitForExit();

            System.Threading.Thread.Sleep(2000);

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
