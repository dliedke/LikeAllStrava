using OpenQA.Selenium;
using System.Text.Json;
using System.Diagnostics;
using OpenQA.Selenium.Chrome;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium.Support.UI;

namespace LikeAllStrava
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Load configuration file
                IConfiguration config = new ConfigurationBuilder()
                                           .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                                           .AddEnvironmentVariables()
                                           .AddCommandLine(args)
                                           .Build();
                StravaSettings stravaSettings = config.GetRequiredSection("StravaSettings").Get<StravaSettings>();

                // If we still don't have login data, get from the use and save it
                if (string.IsNullOrEmpty(stravaSettings.Login))
                {
                    // Ask for login
                    while (string.IsNullOrEmpty(stravaSettings.Login))
                    {
                        Console.WriteLine("Please enter your Strava login (email):");
                        stravaSettings.Login = Console.ReadLine();
                    }
                    stravaSettings.Login = Encryption.EncryptString(stravaSettings.Login);

                    // Ask for password
                    while (string.IsNullOrEmpty(stravaSettings.Password))
                    {
                        Console.WriteLine("Please enter your Strava password:");
                        stravaSettings.Password = Console.ReadLine();
                    }
                    stravaSettings.Password = Encryption.EncryptString(stravaSettings.Password);

                    // Ask for full name
                    while (string.IsNullOrEmpty(stravaSettings.FullName))
                    {
                        Console.WriteLine("Please enter your full name in Strava:");
                        stravaSettings.FullName = Console.ReadLine();
                    }
                    stravaSettings.FullName = Encryption.EncryptString(stravaSettings.FullName);

                    // Save everything
                    Settings settings = new();
                    settings.StravaSettings = stravaSettings;
                    SaveConfigFile(settings);
                }

                // Strava login, password and full name
                string? login = Encryption.DecryptString(stravaSettings.Login);
                string? password = Encryption.DecryptString(stravaSettings.Password);
                string? fullName = Encryption.DecryptString(stravaSettings.FullName);

                // Close all ChromeDrivers open
                CloseAllChromeDrivers();

                // Initialize ChromeDriver with no logs
                string currentRuntimeDirectory = AppContext.BaseDirectory;
                ChromeDriverService service = ChromeDriverService.CreateDefaultService(currentRuntimeDirectory);
                service.SuppressInitialDiagnosticInformation = true;  // Disable logs
                service.EnableVerboseLogging = false;                 // Disable logs
                service.EnableAppendLog = false;                      // Disable logs
                service.HideCommandPromptWindow = true;               // Hide window
                ChromeOptions options = new();
                options.AddArgument("start-maximized");               // Start Chrome window maximized
                options.AddArgument("--no-sandbox");                  // Use same profile as the user
                options.AddArgument("--disable-gpu");                 // Disable hardware acceleration because it shows washed out Strava sometimes
                IWebDriver driver = new ChromeDriver(service, options);
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

                Console.WriteLine("Beginning Strava Login...");

                int totalCountStravaLoad = 0;

            retryStravaLoad:

                try
                {
                    // Error loading Strava (tried 3 times)
                    if (totalCountStravaLoad == 3)
                    {
                        Console.WriteLine("Error: Could not load Strava.");
                        Environment.Exit(-1);
                    }

                    // Try to load Strava login screen
                    driver.Url = "https://www.strava.com/login";

                    // Accept cookies button click
                    WebDriverExtensions.WaitExtension.WaitUntilElement(driver, By.CssSelector(".btn-accept-cookie-banner"), 2);
                    var acceptCookiesButton = driver.FindElement(By.CssSelector(".btn-accept-cookie-banner"));
                    acceptCookiesButton.Click();
                }
                catch
                {
                    totalCountStravaLoad++;
                    goto retryStravaLoad;
                }

                // Set email for login
                var emailText = driver.FindElement(By.Id("email"));
                emailText.SendKeys(login);

                // Set password for login
                var passwordText = driver.FindElement(By.Id("password"));
                passwordText.SendKeys(password);

                // Click in the login button
                var loginButton = driver.FindElement(By.Id("login-button"));
                loginButton.Click();

                // Wait a but and check if page loaded
                WebDriverExtensions.WaitExtension.WaitUntilElement(driver, By.XPath("//*[@data-testid='entry-header']"), 15);
                Console.WriteLine("Completed Strava login");

                // Regex to check if workout is from own user
                Regex regexOwnWorkout = new($@"<a href=""/athletes/[\d]+"" data-testid=""owners-name"">{fullName}</a>", RegexOptions.Compiled);

            retry:

                try
                {
                    // Find all like buttons not yet clicked (svg html tags)
                    var likeElements = driver.FindElements(By.CssSelector("[data-testid='unfilled_kudos']"));
                    foreach (var element in likeElements)
                    {
                        try
                        {
                            // The unfilled_kudos is an svg, retrieve the parent button
                            IWebElement button;
                            button = GetParentElement(element);

                            // Get the card html of the workout to
                            // check if it is not owns user workout
                            var element1 = GetParentElement(GetParentElement(GetParentElement(GetParentElement(GetParentElement(button)))));
                            var str = element1.GetAttribute("innerHTML");

                            // Check if this is not own user workout
                            if (!regexOwnWorkout.IsMatch(str))
                            {
                                // Scroll to the like button
                                Console.Write("Finding workout to give kudos...");
                                ScrollToElement(js, button);
                                
                                // Click in the like button and waits 3s to not be
                                // blocked by Strava
                                js.ExecuteScript("var evt = document.createEvent('MouseEvents');" + "evt.initMouseEvent('click',true, true, window, 0, 0, 0, 0, 0, false, false, false, false, 0,null);" + "arguments[0].dispatchEvent(evt);", button);
                                Console.WriteLine("LIKED!");
                                System.Threading.Thread.Sleep(3000);
                            }
                        }
                        catch { }
                    }
                }
                catch { }

                // Scroll to the bottom of the page to load more content and wait
                Console.WriteLine("Scrolling to load more content...");
                bool pageFinished = ScrollToBottom(driver, js);

                // Repeat scroll until no more new workouts on page are found
                if (!pageFinished)
                {
                    goto retry;
                }

                Console.WriteLine("Finished! Thanks!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Automation error (maybe wrong login data?): " + ex.ToString());
            }
            finally
            {
                // Close all ChromeDrivers open and exit
                CloseAllChromeDrivers();
            }
        }

        private static void SaveConfigFile(Settings settings)
        {
            // Write idented json file
            var jsonWriteOptions = new JsonSerializerOptions()
            {
                WriteIndented = true
            };
            jsonWriteOptions.Converters.Add(new JsonStringEnumConverter());

            // Serialize settings and save in appsettings.json in the application path
            var newJson = JsonSerializer.Serialize(settings, jsonWriteOptions);
            var appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            File.WriteAllText(appSettingsPath, newJson);
        }

        public static IWebElement GetParentElement(IWebElement e)
        {
            return e.FindElement(By.XPath(".."));
        }

        private static void ScrollToElement(IJavaScriptExecutor js, IWebElement element)
        {
            try
            {
                if (element.Location.Y > 200)
                {
                    js.ExecuteScript($"window.scrollTo({0}, {element.Location.Y - 600 })");
                }
            }
            catch { }
        }

        private static void CloseAllChromeDrivers()
        {
            // Close all ChromeDrivers open
            Console.WriteLine("Closing all ChromeDriver processes open...");
            Process process = Process.Start("taskkill", "/F /IM chromedriver.exe /T");
            process.WaitForExit();
            Console.WriteLine("DONE!");
        }

        private static bool ScrollToBottom(IWebDriver driver, IJavaScriptExecutor js)
        {
            // Get the cards to find total number of workouts in the page
            var cards = driver.FindElements(By.CssSelector(".Feed--entry-container--ntrEd"));
            int totalCardsWorkout = cards.Count;

            // Scroll to the end of the page
            js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");

            int retries = 0;
            wait:

            // Get the cards to find total number of workouts in the page now
            cards = driver.FindElements(By.CssSelector(".Feed--entry-container--ntrEd"));
            int totalCardsNow = cards.Count;

            // Check if more workouts were loaded
            // if not, wait a bit more
            if (totalCardsNow == totalCardsWorkout && retries < 10)
            {
                System.Threading.Thread.Sleep(500);
                retries++;
                goto wait;
            }

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