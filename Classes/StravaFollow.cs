using OpenQA.Selenium;

using _s = LikeAllStrava.ShraredObjects;

namespace LikeAllStrava
{
    public class StravaFollow 
    {
        public static void FollowPeople(string url)
        {
            // Add page in the url as parameter to go through all pages
            url += "&page={0}";
            int page = 1;
            int noMoreButtonsCount = 0;

            // Loop through all the pages
            while (true)
            {
                // Navigate to the athlete page to follow more people
                Console.WriteLine($"Following more people (page {page})...");
                _s.ChromeDriver.Url = String.Format(url, page);
                System.Threading.Thread.Sleep(2000);

                // Get all the "Request to Follow" and "Follow" buttons on the page
                var requestToFollowButtons = _s.ChromeDriver.FindElements(By.XPath("//*[@data-state='follow_with_approval']"));
                var followButtons = _s.ChromeDriver.FindElements(By.XPath("//*[@data-state='follow']"));

                // Get all unfollow buttons
                var unfollowButtons = _s.ChromeDriver.FindElements(By.XPath("//*[@data-state='unfollow']"));

                // Get all unfollow for approval buttons
                var unfollowForApprovalButtons = _s.ChromeDriver.FindElements(By.XPath("//*[@data-state='unfollow_for_approval']"));

                // No more follow buttons and just one unfollow button, so exit
                if ((requestToFollowButtons == null && followButtons == null && unfollowButtons == null) ||
                   (requestToFollowButtons?.Count == 0 && followButtons?.Count == 0 && (unfollowButtons?.Count == 1 || unfollowForApprovalButtons?.Count == 1)))
                {
                    break;
                }

                // Just in case we are not yet following the main athelete, so count to 3 pages and exit
                if ((requestToFollowButtons == null && followButtons == null && unfollowButtons == null) ||
                   (requestToFollowButtons?.Count == 0 && followButtons?.Count == 1 && unfollowButtons?.Count == 0))
                {
                    noMoreButtonsCount++;
                }

                if (noMoreButtonsCount == 3)
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
    }
}
