using TextCopy;
using OpenQA.Selenium;
using System.Text.RegularExpressions;

using _s = LikeAllStrava.ShraredObjects;

namespace LikeAllStrava
{
    internal class StravaCongrats
    {
        // Regex to get distance in KMs
        private static readonly Regex _regexFindKmsPT = new($@"Distância<\/span><div class=""Stat--stat-value--g-Ge3 "">([\d,]+)<abbr class=""unit"" title=""quilômetros""> km", RegexOptions.Compiled);
        private static readonly Regex _regexFindKmsEN = new($@"Distance<\/span><div class=""Stat--stat-value--g-Ge3 "">([\d,]+)<abbr class=""unit"" title=""kilometers""> km", RegexOptions.Compiled);

        // Regex to get athlete name
        private static readonly Regex _regexAthleteName = new($@"<a href=""\/athletes\/[\d]+"" data-testid=""owners-name"">(.+?)<\/a>", RegexOptions.Compiled);

        public static void CongratsComment()
        {
            // Regex to check if the workout is from own user so it should not be liked
            Regex _regexOwnWorkout = new($@"<a href=""/athletes/[\d]+"" data-testid=""owners-name"">{_s.FullName}</a>", RegexOptions.Compiled);

            // Load maximum of entries at once
            _s.ChromeDriver.Url = "https://www.strava.com/dashboard?num_entries=600";

            // Wait a bit and check if page is loaded finding an element
            WebDriverExtensions.WaitExtension.WaitUntilElement(_s.ChromeDriver, By.XPath("//*[@data-testid='web-feed-entry']"), 15);

            try
            {
                // Find all comment buttons
                var addCommentElements = _s.ChromeDriver.FindElements(By.CssSelector("[data-testid='comment_button']"));
                foreach (var addCommentButton in addCommentElements)
                {
                    try
                    {
                        // Retrieve the parent button
                        IWebElement button;
                        button = Utilities.GetParentElement(addCommentButton);

                        // Get the card html of the workout
                        var element1 = Utilities.GetParentElement(Utilities.GetParentElement(Utilities.GetParentElement(Utilities.GetParentElement(button))));
                        var str = element1.GetAttribute("innerHTML");

                        // Check if this is not own user workout
                        // And this is a run workout
                        if (!_regexOwnWorkout.IsMatch(str) &&
                            (str.Contains(@"title=""Corrida""") ||
                             str.Contains(@"title=""Run""")))
                        {
                            // Find total KMs ran
                            Match matchKms = _regexFindKmsPT.Match(str);
                            if (!matchKms.Success)
                            {
                                matchKms = _regexFindKmsEN.Match(str);
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
                                        Match athleteNameMatch = _regexAthleteName.Match(str);
                                        if (athleteNameMatch.Success && matchKms.Groups.Count > 1)
                                        {
                                            athleteFirstname = athleteNameMatch.Groups[1].Value.Split(' ')[0];
                                        }

                                        // Replace the [name] tag with athlete first name found
                                        string messageCongrats = _s.MessageCongratsComment.Replace("[name]", athleteFirstname);

                                        // Scroll to the comment button
                                        Console.WriteLine("Found run workout to add comment...");
                                        Utilities.ScrollToElement(addCommentButton);
                                        System.Threading.Thread.Sleep(1000);

                                        // Get the card html of the workout
                                        var element2 = Utilities.GetParentElement(Utilities.GetParentElement(Utilities.GetParentElement(Utilities.GetParentElement(button))));
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
                                        var elementCommentTextBox = _s.ChromeDriver.FindElement(By.XPath("//textarea[@placeholder='Adicione um comentário, @ para mencionar']"));
                                        if (elementCommentTextBox != null)
                                        {
                                            // Scroll to comment box 
                                            Utilities.ScrollToElement(elementCommentTextBox);
                                            System.Threading.Thread.Sleep(1000);

                                            // Use clipboard to copy+paste comment message in textarea to allow emoticons
                                            ClipboardService.SetText(messageCongrats);
                                            elementCommentTextBox.SendKeys(OpenQA.Selenium.Keys.Control + "v");

                                            // Find button to post and click on it
                                            var publishButton = _s.ChromeDriver.FindElement(By.CssSelector("[data-testid='post-comment-btn']"));
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
            finally
            {
                // Empty the clipboard in the end
                ClipboardService.SetText("");
            }
        }
    }
}
