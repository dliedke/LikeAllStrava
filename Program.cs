// Application to automate like all workouts in the Strava feed and more :)

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
        private static StravaSettings _stravaSettings = new();
        private static string _login = string.Empty;
        private static string _password = string.Empty;
        private static string _fullName = string.Empty;
        private static string _urlFollowPeople = string.Empty;
        private static string _messageCongratsLongRun = string.Empty;

        static void Main(string[] args)
        {
            try
            {
                // Validate command-line parameters if we have them
                if (args.Length > 0)
                {
                    // Parameters can be followpeople and then URL
                    // Example of url: https://www.strava.com/athletes/9954999/follows?type=following
                    // If we don't have the url, ask the user

                    if (args.Length == 1 && args[0] == "followpeople")
                    {
                        Console.WriteLine("\r\nPlease enter Strava URL of athlete when in the \"Following\" tab:");
                        _urlFollowPeople = Console.ReadLine();
                    }
                    if (args.Length == 2 && args[0] == "followpeople")
                    {
                        _urlFollowPeople = args[1];
                    }

                    // Parameters can be congratslongrun and then message (placeholder [name] will be first name of the athlete)
                    // Example of parameters: congratslongrun "Congratulations for the long run [name]!"
                    // If we don't have the message, ask the user

                    if (args.Length == 1 && args[0] == "congratslongrun")
                    {
                        Console.WriteLine("\r\nPlease enter congratulations message for the long run (use [name] as first name of the athlete to be replaced):");
                        _messageCongratsLongRun = Console.ReadLine();
                    }
                    if (args.Length == 2 && args[0] == "congratslongrun")
                    {
                        _messageCongratsLongRun = args[1];
                    }
                }

                // Read config file
                InitializeConfig(args);

                // Ask user for Strava login data if required first time
                RequestLoginData();

                // Login into Strava platform
                StravaLogin();

                // Check if we need to follow more people
                if (args.Length > 0 && args[0] == "followpeople" && !string.IsNullOrEmpty(_urlFollowPeople))
                {
                    // Call automation to follow more people
                    FollowPeople(_urlFollowPeople);
                }
                // Check if we need to congratulate the people for the long run
                else if (args.Length > 0 && args[0] == "congratslongrun" && !string.IsNullOrEmpty(_messageCongratsLongRun))
                {
                    // Call automation to add congratulations comments for long runs
                    CongratsLongRun();
                }
                else 
                {
                    // Like all the workouts in the Strava newsfeed
                    LikeWorkouts();
                }

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
            // Load configuration file with login, password and full name for Strava
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
            _login = Encryption.DecryptString(_stravaSettings.Login);
            _password = Encryption.DecryptString(_stravaSettings.Password);
            _fullName = Encryption.DecryptString(_stravaSettings.FullName);

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
                    Console.WriteLine("Error: Could not login into Strava.");
                    CloseAllChromeDrivers();
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
            WebDriverExtensions.WaitExtension.WaitUntilElement(_chromeDriver, By.XPath("//*[@data-testid='web-feed-entry']"), 15);

            // Refresh page because sometimes the pictures are not loaded 
            _chromeDriver.Navigate().Refresh();

            Console.WriteLine("Completed Strava login");
        }

        private static void CongratsLongRun()
        {
            // Regex to check if the workout is from own user so it should not be liked
            Regex regexOwnWorkout = new($@"<a href=""/athletes/[\d]+"" data-testid=""owners-name"">{_fullName}</a>", RegexOptions.Compiled);

            // Regex to get distance in KMs
            Regex regexFindKmsPT = new($@"Distância<\/span><div class=""Stat--stat-value--g-Ge3 "">([\d,]+)<abbr class=""unit"" title=""quilômetros""> km");
            Regex regexFindKmsEN = new($@"Distance<\/span><div class=""Stat--stat-value--g-Ge3 "">([\d,]+)<abbr class=""unit"" title=""kilometers""> km");

            // Regex to get athlete name
            Regex regexAthleteName = new($@"<a href=""\/athletes\/[\d]+"" data-testid=""owners-name"">([\w ]+)<\/a>");

            // Load maximum of entries at once
            _chromeDriver.Url = "https://www.strava.com/dashboard?num_entries=600";

            // Wait a bit and check if page is loaded finding an element
            WebDriverExtensions.WaitExtension.WaitUntilElement(_chromeDriver, By.XPath("//*[@data-testid='web-feed-entry']"), 15);

            try
            {
                // Find all comment buttons
                var addCommentElements = _chromeDriver.FindElements(By.CssSelector("[data-testid='comment_button']"));
                foreach (var addCommentButton in addCommentElements)
                {
                    try
                    {
                        // Retrieve the parent button
                        IWebElement button;
                        button = GetParentElement(addCommentButton);

                        // Get the card html of the workout
                        var element1 = GetParentElement(GetParentElement(GetParentElement(GetParentElement(button))));
                        var str = element1.GetAttribute("innerHTML");

                        // Check if this is not own user workout
                        // And this is a run workout
                        if (!regexOwnWorkout.IsMatch(str) && 
                            (str.Contains(@"title=""Corrida""") ||
                             str.Contains(@"title=""Run""")))
                        {
                            // Find total KMs ran
                            Match matchKms = regexFindKmsPT.Match(str);
                            if (!matchKms.Success)
                            {
                                matchKms = regexFindKmsEN.Match(str);
                            }
                            if (matchKms.Success)
                            {
                                if (matchKms.Groups.Count > 1)
                                {
                                    // If the workout was 10km or more
                                    bool successParsingKms = float.TryParse(matchKms.Groups[1].Value, out float kms);
                                    if (successParsingKms && kms >= 10)
                                    {
                                        // Retrieve athlete first name
                                        string athleteFirstname = string.Empty;
                                        Match athleteNameMatch = regexAthleteName.Match(str);
                                        if (athleteNameMatch.Success && matchKms.Groups.Count > 1)
                                        {
                                            athleteFirstname = athleteNameMatch.Groups[1].Value.Split(' ')[0];
                                        }

                                        // Replace the [name] tag with athlete first name found
                                        string messageCongrats = _messageCongratsLongRun.Replace("[name]", athleteFirstname);

                                        // Scroll to the comment button
                                        Console.WriteLine("Found long run to add comment...");
                                        ScrollToElement(addCommentButton);
                                        System.Threading.Thread.Sleep(1000);

                                        // Get the card html of the workout
                                        var element2 = GetParentElement(GetParentElement(GetParentElement(GetParentElement(button))));
                                        var str2 = element2.GetAttribute("innerHTML");

                                        // If we already commented, do not add duplicated comment
                                        if (str2.Contains(messageCongrats))
                                        {
                                            continue;
                                        }

                                        // Click in the comment button then waits 1s 
                                        addCommentButton.Click();
                                        Console.WriteLine("Adding comment...");
                                        System.Threading.Thread.Sleep(1000);

                                        // Find the comment box
                                        var elementCommentTextBox = _chromeDriver.FindElement(By.XPath("//textarea[@placeholder='Adicione um comentário, @ para mencionar']"));
                                        if (elementCommentTextBox != null)
                                        {
                                            // Scroll to comment box 
                                            ScrollToElement(elementCommentTextBox);
                                            System.Threading.Thread.Sleep(1000);

                                            // Type the comment message
                                            elementCommentTextBox.SendKeys(messageCongrats);

                                            // Find button to post and click on it
                                            var publishButton = _chromeDriver.FindElement(By.CssSelector("[data-testid='post-comment-btn']"));
                                            if (publishButton != null)
                                            {
                                                // Publish the comment
                                                publishButton.Click();
                                                Console.WriteLine("Comment published!");

                                                // Close the comment box
                                                addCommentButton.Click();

                                                // Wait 10s for user to review the comment
                                                System.Threading.Thread.Sleep(10000);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
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
                            ClickElementJavascript(button);
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
            // Add page in the url as parameter to go through all pages
            url += "&page={0}";
            int page = 1;

            // Loop through all the pages
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

                // No more follow buttons and just one unfollow button, so exit
                if ((requestToFollowButtons == null && followButtons == null && unfollowButtons == null) ||
                   (requestToFollowButtons?.Count == 0 && followButtons?.Count == 0 && unfollowButtons?.Count == 1))
                {
                    break;
                }

                // If we have "Request to Follow" buttons, click on them and wait 2s
                if (requestToFollowButtons != null && requestToFollowButtons.Count > 0)
                {
                    foreach (var button in requestToFollowButtons)
                    {
                        button.Click();
                        System.Threading.Thread.Sleep(2000);
                    }
                }

                // If we have "Follow" buttons, click on them and wait 2s
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

            Console.WriteLine($"Completed following more people! Thanks!");
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

        public static void ClickElementJavascript(IWebElement element)
        {
            _javascriptExecutor.ExecuteScript("var evt = document.createEvent('MouseEvents');" + "evt.initMouseEvent('click',true, true, window, 0, 0, 0, 0, 0, false, false, false, false, 0,null);" + "arguments[0].dispatchEvent(evt);", element);
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
                    // Scroll page until the element but showing the workout pictures
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