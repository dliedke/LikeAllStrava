using OpenQA.Selenium;

using _s = LikeAllStrava.ShraredObjects;

namespace LikeAllStrava
{
    public class StravaLoad
    {
        public static void RequestFullName()
        {
            // If we still don't have login data, get from the user and save it
            if (_s.StravaSettings != null && string.IsNullOrEmpty(_s.StravaSettings.FullName))
            {
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

        public static void Load()
        {
            // Decrypt the Strava full name
            _s.FullName = Encryption.DecryptString(_s.StravaSettings.FullName);

            // Close all Edge browsers open
            EdgeDriverControl.CloseAllEdgeBrowserWindows();

            // Close all EdgeDriver processes open
            EdgeDriverControl.CloseAllEdgeDrivers();

            // Initialize EdgeDriver with no logs at all
            EdgeDriverControl.InitializeEdgeDriver();

            // Navigate to Strava dashboard page
            _s.EdgeDriver.Navigate().GoToUrl("https://www.strava.com/dashboard");

            // Wait for feed to load
            WebDriverExtensions.WaitExtension.WaitUntilElement(_s.EdgeDriver, By.XPath("//*[@data-testid='web-feed-entry']"), 15);

            Console.WriteLine("Completed Strava Load");
        }
    }
}
