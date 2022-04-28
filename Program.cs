// Program to like all workouts in the Strava feed

using OpenQA.Selenium;
using System.Text.Json;
using System.Diagnostics;
using OpenQA.Selenium.Chrome;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace LikeAllStrava
{
    public class Program
    {
        private static IWebDriver _chromeDriver;
        private static IJavaScriptExecutor _javascriptExecutor;
        private static StravaSettings? _stravaSettings;
        private static string? _login;
        private static string? _password;
        private static string? _fullName;

        static void Main(string[] args)
        {
            try
            {
                // Validate command-line parameters if we have them
                if (args.Length > 0)
                {
                    // Parameters should be followpeople and then URL
                    // Example of url: https://www.strava.com/athletes/9954999/follows?type=following
                    string url = string.Empty;
                    if (!(args.Length == 2 && args[0] == "followpeople" && !string.IsNullOrEmpty(args[1])))
                    {
                        Console.WriteLine("\r\nUsage: LikeAllStrava followpeople [url of athlete when clicking in following tab]\r\n");
                        return;
                    }
                }

                // Read config file
                InitializeConfig(args);

                // Ask user for Strava login data if required first time
                RequestLoginData();

                // Login into Strava platform
                StravaLogin();

                // Check if we need to follow more people
                if (args.Length > 0)
                {
                    string url = string.Empty;
                    if (args.Length == 2 && args[0] == "followpeople" && !string.IsNullOrEmpty(args[1]))
                    {
                        url = args[1];
                        FollowPeople(url);
                        return;
                    }
                }

                // Like all the workouts in the Strava newsfeed
                LikeWorkouts();

                Console.WriteLine("Finished! Thanks!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Automation error (maybe wrong login data?): " + ex.ToString());
            }
            finally
            {
                // Close all ChromeDriver processes and exit
                CloseAllChromeDrivers();
            }
        }

        private static void InitializeConfig(string[] args)
        {
            // Load configuration file with login, password and full name in strava
            IConfiguration config = new ConfigurationBuilder()
                                       .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                                       .AddEnvironmentVariables()
                                       .AddCommandLine(args)
                                       .Build();

            _stravaSettings = config.GetRequiredSection("StravaSettings").Get<StravaSettings>();
        }

        private static void RequestLoginData()
        {
            // If we still don't have login data, get from the user and save it
            if (_stravaSettings!=null && string.IsNullOrEmpty(_stravaSettings.Login))
            {
                // Ask user for Strava login and encrypt it
                while (string.IsNullOrEmpty(_stravaSettings.Login))
                {
                    Console.WriteLine("Please enter your Strava login (email):");
                    _stravaSettings.Login = Console.ReadLine();
                }
                _stravaSettings.Login = Encryption.EncryptString(_stravaSettings.Login);

                // Ask user for Strava password and encrypt it
                while (string.IsNullOrEmpty(_stravaSettings.Password))
                {
                    Console.WriteLine("Please enter your Strava password:");
                    _stravaSettings.Password = Console.ReadLine();
                }
                _stravaSettings.Password = Encryption.EncryptString(_stravaSettings.Password);

                // Ask user for full name in Strava and encrypt it
                while (string.IsNullOrEmpty(_stravaSettings.FullName))
                {
                    Console.WriteLine("Please enter your full name in Strava:");
                    _stravaSettings.FullName = Console.ReadLine();
                }
                _stravaSettings.FullName = Encryption.EncryptString(_stravaSettings.FullName);

                // Save everything in the appsettings.json configuration file
                Settings settings = new();
                settings.StravaSettings = _stravaSettings;
                SaveConfigFile(settings);
            }
        }
    
        private static void InitializeChromeDriver()
        {
            // Initialize ChromeDriver with no logs at all
            string currentRuntimeDirectory = AppContext.BaseDirectory;
            ChromeDriverService service = ChromeDriverService.CreateDefaultService(currentRuntimeDirectory);
            service.SuppressInitialDiagnosticInformation = true;  // Disable logs
            service.EnableVerboseLogging = false;                 // Disable logs
            service.EnableAppendLog = false;                      // Disable logs
            service.HideCommandPromptWindow = true;               // Hide window
            ChromeOptions options = new();
            options.AddArgument("start-maximized");               // Start Chrome window maximized
            options.AddArgument("--no-sandbox");                  // Use same profile as the user
            options.AddArgument("--disable-gpu");                 // Disable hardware acceleration because it shows washed out Strava sometimes in HDR screens
            _chromeDriver = new ChromeDriver(service, options);
            _javascriptExecutor = (IJavaScriptExecutor)_chromeDriver;
        }

        private static void StravaLogin()
        {
            // Decrypt the Strava login, password and full name
            _login = Encryption.DecryptString(_stravaSettings?.Login);
            _password = Encryption.DecryptString(_stravaSettings?.Password);
            _fullName = Encryption.DecryptString(_stravaSettings?.FullName);

            // Close all ChromeDriver processes open
            CloseAllChromeDrivers();

            // Initialize ChromeDriver with no logs at all
            InitializeChromeDriver();

            Console.WriteLine("Beginning Strava Login...");

            int totalCountStravaLoad = 0;

        retryStravaLoad:

            try
            {
                // Error loading Strava (tried 3 times already)
                if (totalCountStravaLoad == 3)
                {
                    Console.WriteLine("Error: Could not load Strava.");
                    Environment.Exit(-1);
                }

                // Try to load Strava login screen
                _chromeDriver.Url = "https://www.strava.com/login";

                // "Accept Cookies" button click
                WebDriverExtensions.WaitExtension.WaitUntilElement(_chromeDriver, By.CssSelector(".btn-accept-cookie-banner"), 2);
                var acceptCookiesButton = _chromeDriver.FindElement(By.CssSelector(".btn-accept-cookie-banner"));
                acceptCookiesButton.Click();
            }
            catch
            {
                totalCountStravaLoad++;
                goto retryStravaLoad;
            }

            // Set email for login
            var emailText = _chromeDriver.FindElement(By.Id("email"));
            emailText.SendKeys(_login);

            // Set password for login
            var passwordText = _chromeDriver.FindElement(By.Id("password"));
            passwordText.SendKeys(_password);

            // Click in the login button
            var loginButton = _chromeDriver.FindElement(By.Id("login-button"));
            loginButton.Click();

            // Wait a bit and check if page is loaded finding an element
            WebDriverExtensions.WaitExtension.WaitUntilElement(_chromeDriver, By.XPath("//*[@data-testid='entry-header']"), 15);
            Console.WriteLine("Completed Strava login");
        }

        private static void LikeWorkouts()
        {
            // Regex to check if the workout is from own user so it should not be liked
            Regex regexOwnWorkout = new($@"<a href=""/athletes/[\d]+"" data-testid=""owners-name"">{_fullName}</a>", RegexOptions.Compiled);

        retry:

            try
            {
                // Find all like buttons not yet clicked (svg html tags)
                var likeElements = _chromeDriver.FindElements(By.CssSelector("[data-testid='unfilled_kudos']"));
                foreach (var element in likeElements)
                {
                    try
                    {
                        // The unfilled_kudos is an svg, retrieve the parent button to click
                        IWebElement button;
                        button = GetParentElement(element);

                        // Get the card html of the workout
                        var element1 = GetParentElement(GetParentElement(GetParentElement(GetParentElement(GetParentElement(button)))));
                        var str = element1.GetAttribute("innerHTML");

                        // Check if this is not own user workout
                        if (!regexOwnWorkout.IsMatch(str))
                        {
                            // Scroll to the like button
                            Console.Write("Finding workout to give kudos...");
                            ScrollToElement(element);

                            // Click in the like button using javascript
                            // then waits 3s to not be blocked by Strava because of automation
                            _javascriptExecutor.ExecuteScript("var evt = document.createEvent('MouseEvents');" + "evt.initMouseEvent('click',true, true, window, 0, 0, 0, 0, 0, false, false, false, false, 0,null);" + "arguments[0].dispatchEvent(evt);", button);
                            Console.WriteLine("LIKED!");
                            System.Threading.Thread.Sleep(3000);
                        }
                    }
                    catch { }
                }
            }
            catch { }

            // Scroll to the bottom of the page to load more content
            Console.WriteLine("Scrolling to load more content...");
            bool pageFinished = ScrollToBottom();

            // Repeat scroll until no more new workouts are found on the page
            if (!pageFinished)
            {
                goto retry;
            }
        }

        private static void FollowPeople(string url)
        {
            // Add page in the url as parameter
            url += "&page={0}";
            int page = 1;

            // Loop throgh all the pages
            while (true)
            {
                // Navigate to the athlete page to follow more people
                Console.WriteLine($"Following more people (page {page})...");
                _chromeDriver.Url = String.Format(url, page);
                System.Threading.Thread.Sleep(2000);

                // Get all the "Request to Follow" and "Follow" buttons on the page
                var requestToFollowButtons = _chromeDriver.FindElements(By.XPath("//*[@data-state='follow_with_approval']"));
                var followButtons = _chromeDriver.FindElements(By.XPath("//*[@data-state='follow']"));

                // Get all unfollow buttons
                var unfollowButtons = _chromeDriver.FindElements(By.XPath("//*[@data-state='unfollow']"));

                // No more follow/unfollow buttons, so exit
                if ((requestToFollowButtons == null && followButtons == null && unfollowButtons == null) ||
                   (requestToFollowButtons?.Count == 0 && followButtons?.Count == 0 && unfollowButtons?.Count == 0))
                {
                    break;
                }

                // If we have request to follow buttons, click on them and wait 2s
                if (requestToFollowButtons != null && requestToFollowButtons.Count > 0)
                {
                    foreach (var button in requestToFollowButtons)
                    {
                        button.Click();
                        System.Threading.Thread.Sleep(2000);
                    }
                }

                // If we have follow buttons, click on them and wait 2s
                if (followButtons != null && followButtons.Count > 0)
                {
                    foreach (var button in followButtons)
                    {
                        button.Click();
                        System.Threading.Thread.Sleep(2000);
                    }
                }

                // Increase page count to go the next page
                page++;
            }

            Console.WriteLine($"Complete following more people! Thanks!");
        }

        private static void SaveConfigFile(Settings settings)
        {
            // Write idented json file
            var jsonWriteOptions = new JsonSerializerOptions()
            {
                WriteIndented = true
            };
            jsonWriteOptions.Converters.Add(new JsonStringEnumConverter());

            // Serialize settings with login data
            // and save in appsettings.json in the application path
            var newJson = JsonSerializer.Serialize(settings, jsonWriteOptions);
            var appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            File.WriteAllText(appSettingsPath, newJson);
        }

        public static IWebElement GetParentElement(IWebElement e)
        {
            // Retrieve the parent DOM element from the HTML
            return e.FindElement(By.XPath(".."));
        }

        private static void ScrollToElement(IWebElement element)
        {
            try
            {
                if (element.Location.Y > 200)
                {
                    // Scroll page until the element
                    _javascriptExecutor.ExecuteScript($"window.scrollTo({0}, {element.Location.Y - 600 })");
                }
            }
            catch { }
        }

        private static void CloseAllChromeDrivers()
        {
            // Close all ChromeDriver processes open
            Console.WriteLine("Closing all ChromeDriver processes open...");
            Process process = Process.Start("taskkill", "/F /IM chromedriver.exe /T");
            process.WaitForExit();
            Console.WriteLine("DONE!");
        }

        private static bool ScrollToBottom()
        {
            // Get the cards to find total number of workouts in the page
            var cards = _chromeDriver.FindElements(By.CssSelector(".Feed--entry-container--ntrEd"));
            int totalCardsWorkout = cards.Count;

            // Scroll to the end of the page
            _javascriptExecutor.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");

            int retries = 0;

        wait:

            // Get the cards to find total number of workouts in the page now
            cards = _chromeDriver.FindElements(By.CssSelector(".Feed--entry-container--ntrEd"));
            int totalCardsNow = cards.Count;

            // Check if more workouts were loaded
            // if not, wait a bit more
            if (totalCardsNow == totalCardsWorkout && retries < 10)
            {
                System.Threading.Thread.Sleep(500);
                retries++;
                goto wait;
            }

            // After 5s if no more workouts were find we are done
            if (retries == 10)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}