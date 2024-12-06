using OpenQA.Selenium;

using _s = LikeAllStrava.ShraredObjects;

namespace LikeAllStrava
{
    public class StravaLogin
    {
        private static string _login;
        private static string _password;

        public static void RequestLoginData()
        {
            // If we still don't have login data, get from the user and save it
            if (_s.StravaSettings != null && string.IsNullOrEmpty(_s.StravaSettings.Login))
            {
                // Ask user for Strava login and encrypt it
                while (string.IsNullOrEmpty(_s.StravaSettings.Login))
                {
                    Console.WriteLine("Please enter your Strava login (email):");
                    _s.StravaSettings.Login = Console.ReadLine();
                }
                _s.StravaSettings.Login = Encryption.EncryptString(_s.StravaSettings.Login);

                // Ask user for Strava password and encrypt it
                while (string.IsNullOrEmpty(_s.StravaSettings.Password))
                {
                    Console.WriteLine("Please enter your Strava password:");
                    _s.StravaSettings.Password = Console.ReadLine();
                }
                _s.StravaSettings.Password = Encryption.EncryptString(_s.StravaSettings.Password);

                // Ask user for full name in Strava and encrypt it
                while (string.IsNullOrEmpty(_s.StravaSettings.FullName))
                {
                    Console.WriteLine("Please enter your full name in Strava:");
                    _s.StravaSettings.FullName = Console.ReadLine();
                }
                _s.StravaSettings.FullName = Encryption.EncryptString(_s.StravaSettings.FullName);

                // Save everything in the appsettings.json configuration file
                Settings settings = new();
                settings.StravaSettings = _s.StravaSettings;
                Utilities.SaveConfigFile(settings);
            }
        }

        public static void Login()
        {
            // Decrypt the Strava login, password and full name
            _login = Encryption.DecryptString(_s.StravaSettings.Login);
            _password = Encryption.DecryptString(_s.StravaSettings.Password);
            _s.FullName = Encryption.DecryptString(_s.StravaSettings.FullName);

            // Close all ChromeDriver processes open
            ChromeDriverControl.CloseAllChromeDrivers();

            // Initialize ChromeDriver with no logs at all
            ChromeDriverControl.InitializeChromeDriver();

            Console.WriteLine("Beginning Strava Login...");

            int totalCountStravaLoad = 0;

        retryStravaLoad:

            try
            {
                if (totalCountStravaLoad == 3)
                {
                    Console.WriteLine("Error: Could not login into Strava.");
                    ChromeDriverControl.CloseAllChromeDrivers();
                    Environment.Exit(-1);
                }

                // Load Strava login screen
                _s.ChromeDriver.Url = "https://www.strava.com/login";
                _s.ChromeDriver.Manage().Window.Maximize();

                // Accept cookies
                WebDriverExtensions.WaitExtension.WaitUntilElement(_s.ChromeDriver, By.CssSelector("[data-cy='accept-cookies']"), 2);
                ((IJavaScriptExecutor)_s.ChromeDriver).ExecuteScript("document.querySelector('[data-cy=\"accept-cookies\"]').click();");
            }
            catch
            {
                totalCountStravaLoad++;
                goto retryStravaLoad;
            }

            // Random number generator for realistic delays
            var random = new Random();

            // Type email with realistic delays
            var emailInput = _s.ChromeDriver.FindElement(By.Id("desktop-email"));
            emailInput.Clear();
            foreach (char c in _login)
            {
                emailInput.SendKeys(c.ToString());
                Thread.Sleep(random.Next(50, 150)); // Random delay between keystrokes
            }

            // Delay between fields like a human would
            Thread.Sleep(random.Next(500, 1000));

            // Type password with realistic delays
            var passwordInput = _s.ChromeDriver.FindElement(By.Id("desktop-current-password"));
            passwordInput.Clear();
            foreach (char c in _password)
            {
                passwordInput.SendKeys(c.ToString());
                Thread.Sleep(random.Next(50, 150));
            }

            // Delay before clicking submit
            Thread.Sleep(random.Next(800, 1200));

            // Click submit button using JavaScript
            ((IJavaScriptExecutor)_s.ChromeDriver).ExecuteScript(
                "document.querySelectorAll('button[type=\"submit\"]')[1].click();"
            );

            // Wait for feed to load
            WebDriverExtensions.WaitExtension.WaitUntilElement(_s.ChromeDriver, By.XPath("//*[@data-testid='web-feed-entry']"), 15);

            // Refresh page because sometimes the pictures are not loaded 
            _s.ChromeDriver.Navigate().Refresh();

            Console.WriteLine("Completed Strava login");
        }
    }
}
