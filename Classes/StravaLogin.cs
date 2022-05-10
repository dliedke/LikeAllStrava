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
                // Error loading Strava (tried 3 times already)
                if (totalCountStravaLoad == 3)
                {
                    Console.WriteLine("Error: Could not login into Strava.");
                    ChromeDriverControl.CloseAllChromeDrivers();
                    Environment.Exit(-1);
                }

                // Try to load Strava login screen
                _s.ChromeDriver.Url = "https://www.strava.com/login";
                _s.ChromeDriver.Manage().Window.Maximize();

                // "Accept Cookies" button click
                WebDriverExtensions.WaitExtension.WaitUntilElement(_s.ChromeDriver, By.CssSelector(".btn-accept-cookie-banner"), 2);
                var acceptCookiesButton = _s.ChromeDriver.FindElement(By.CssSelector(".btn-accept-cookie-banner"));
                acceptCookiesButton.Click();
            }
            catch
            {
                totalCountStravaLoad++;
                goto retryStravaLoad;
            }

            // Set email for login
            var emailText = _s.ChromeDriver.FindElement(By.Id("email"));
            emailText.SendKeys(_login);

            // Set password for login
            var passwordText = _s.ChromeDriver.FindElement(By.Id("password"));
            passwordText.SendKeys(_password);

            // Click in the login button
            var loginButton = _s.ChromeDriver.FindElement(By.Id("login-button"));
            loginButton.Click();

            // Wait a bit and check if page is loaded finding an element
            WebDriverExtensions.WaitExtension.WaitUntilElement(_s.ChromeDriver, By.XPath("//*[@data-testid='web-feed-entry']"), 15);

            // Refresh page because sometimes the pictures are not loaded 
            _s.ChromeDriver.Navigate().Refresh();

            Console.WriteLine("Completed Strava login");
        }
    }
}
