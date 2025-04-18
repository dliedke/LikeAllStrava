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
                // Find all unfilled kudos buttons by looking for buttons that contain the unfilled_kudos SVG
                // Step 1: Get all kudos buttons
                var kudosButtons = _s.EdgeDriver.FindElements(By.CssSelector("button[data-testid='kudos_button']"));

                // Step 2: Filter only those with unfilled kudos SVG
                var unfilledKudosButtons = kudosButtons
                    .Where(button => button
                        .FindElements(By.CssSelector("svg[data-testid='unfilled_kudos']"))
                        .Any())
                    .ToList();

                bool oldApproach = false;

                if (unfilledKudosButtons.Count == 0)
                {
                    // Find all like buttons not yet clicked (svg html tags)
                    kudosButtons = _s.EdgeDriver.FindElements(By.CssSelector("[data-testid='unfilled_kudos']"));
                    unfilledKudosButtons = [.. kudosButtons];
                    oldApproach = true;
                }

                foreach (var element in unfilledKudosButtons)
                {
                    try
                    {
                        // Get the parent button element
                        IWebElement button = element;

                        // Navigate up to find the card container
                        // The new structure goes: button -> mediaActions -> kudosAndComments -> entryFooter -> feedEntry (card)
                        IWebElement card;

                        if (oldApproach == false)
                        {
                            card = button.FindElement(By.XPath("./ancestor::div[@data-testid='web-feed-entry']"));
                            if (card == null)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            // The unfilled_kudos is an svg, retrieve the parent button to click
                            button = Utilities.GetParentElement(element);

                            // Get the card html of the workout
                            card = Utilities.GetParentElement(Utilities.GetParentElement(Utilities.GetParentElement(Utilities.GetParentElement(Utilities.GetParentElement(button)))));
                        }

                        var str = card.GetDomProperty("innerHTML");

                        // Check if this is not own user workout
                        if (!regexOwnWorkout.IsMatch(str))
                        {
                            // Scroll to the like button
                            Console.Write("Finding workout to give kudos...");
                            Utilities.ScrollToElement(element);

                            // Click the like button using javascript
                            Utilities.ClickElementJavascript(button);
                            totalLikes++;
                            Console.WriteLine($"LIKED! ({totalLikes})");

                            // Maximum likes per hour
                            if (totalLikes == 117)
                            {
                                Console.WriteLine("Maximum likes for now reached. Exiting...");
                                return;
                            }

                            // Wait between likes to avoid being blocked
                            Thread.Sleep(3000);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing individual like: {ex.Message}");
                        continue; // Skip this item and continue with the next
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in main like loop: {ex.Message}");
            }

            // Scroll to load more content
            Console.WriteLine("Scrolling to load more content...");
            bool pageFinished = ScrollToBottom();

            // Continue if there are more workouts to load
            if (!pageFinished)
            {
                goto retry;
            }
        }


        private static bool ScrollToBottom()
        {
            // Get the cards to find total number of workouts in the page
            var cards = _s.EdgeDriver.FindElements(By.CssSelector(".------packages-feed-ui-src-features-FeedEntry__entry-container--FPn3K"));
            int totalCardsWorkout = cards.Count;

            if (totalCardsWorkout == 0)
            {
                cards = _s.EdgeDriver.FindElements(By.CssSelector("div[id^='feed-entry-']"));
                totalCardsWorkout = cards.Count;
            }
            // Scroll to the end of the page
            _s.JavascriptExecutor.ExecuteScript("window.scrollTo(0, document.body.scrollHeight-1500);");

            int retries = 0;

        wait:

            // Get the cards to find total number of workouts in the page now
            cards = _s.EdgeDriver.FindElements(By.CssSelector(".------packages-feed-ui-src-features-FeedEntry__entry-container--FPn3K"));
            int totalCardsNow = cards.Count;

            if (totalCardsNow == 0)
            {
                cards = _s.EdgeDriver.FindElements(By.CssSelector("div[id^='feed-entry-']"));
                totalCardsNow = cards.Count;
            }

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
