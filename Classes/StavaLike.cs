using OpenQA.Selenium;
using System.Text.RegularExpressions;

using _s = LikeAllStrava.ShraredObjects;

namespace LikeAllStrava
{
    public class StravaLike 
    {
        public static void LikeWorkouts()
        {
            int totalLikes = 0;

            // Regex to check if the workout is from own user so it should not be liked
            Regex regexOwnWorkout = new($@"<a href=""/athletes/[\d]+"" data-testid=""owners-name"">{_s.FullName}</a>", RegexOptions.Compiled);

        retry:

            try
            {
                // Find all like buttons not yet clicked (svg html tags)
                var likeElements = _s.ChromeDriver.FindElements(By.CssSelector("[data-testid='unfilled_kudos']"));
                foreach (var element in likeElements)
                {
                    try
                    {
                        // The unfilled_kudos is an svg, retrieve the parent button to click
                        IWebElement button;
                        button = Utilities.GetParentElement(element);

                        // Get the card html of the workout
                        var element1 = Utilities.GetParentElement(Utilities.GetParentElement(Utilities.GetParentElement(Utilities.GetParentElement(Utilities.GetParentElement(button)))));
                        var str = element1.GetAttribute("innerHTML");

                        // Check if this is not own user workout
                        if (!regexOwnWorkout.IsMatch(str))
                        {
                            // Scroll to the like button
                            Console.Write("Finding workout to give kudos...");
                            Utilities.ScrollToElement(element);

                            // Click in the like button using javascript
                            // then waits 3s to not be blocked by Strava because of automation
                            Utilities.ClickElementJavascript(button);
                            totalLikes++;
                            Console.WriteLine($"LIKED! ({totalLikes})");

                            // Maximum likes per hour?
                            if (totalLikes == 117)
                            {
                                Console.WriteLine("Maximum likes for now reached. Exiting...");
                                return;
                            }
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


        private static bool ScrollToBottom()
        {
            // Get the cards to find total number of workouts in the page
            var cards = _s.ChromeDriver.FindElements(By.CssSelector(".------packages-feed-ui-src-features-FeedEntry__entry-container--FPn3K"));
            int totalCardsWorkout = cards.Count;

            // Scroll to the end of the page
            _s.JavascriptExecutor.ExecuteScript("window.scrollTo(0, document.body.scrollHeight-1500);");

            int retries = 0;

        wait:

            // Get the cards to find total number of workouts in the page now
            cards = _s.ChromeDriver.FindElements(By.CssSelector(".------packages-feed-ui-src-features-FeedEntry__entry-container--FPn3K"));
            int totalCardsNow = cards.Count;

            // Check if more workouts were loaded
            // if not, wait a bit more
            if (totalCardsNow == totalCardsWorkout && retries < 30)
            {
                System.Threading.Thread.Sleep(500);
                retries++;
                goto wait;
            }

            // After 15s if no more workouts were find we are done
            if (retries == 30)
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
                                                              