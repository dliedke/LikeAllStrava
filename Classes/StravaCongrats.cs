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
                        var str = element1.GetDomProperty("innerHTML");

                        // Check if this is not own user workout
                        // And validate workout type
                        if (!_regexOwnWorkout.IsMatch(str) &&
                            ValidateTrainingType(str))
                        {
                            // Find total KMs for the workout
                            bool successParsingKms = false;
                            float kms = 0;
                            Match matchKms = _regexFindKmsPT.Match(str);
                            if (!matchKms.Success)
                            {
                                matchKms = _regexFindKmsEN.Match(str);
                            }
                            if (matchKms.Success)
                            {
                                if (matchKms.Groups.Count > 1)
                                {
                                    successParsingKms = float.TryParse(matchKms.Groups[1].Value, out kms);
                                }
                            }

                            // If the workout distance is within range or it is swimming workout
                            if ((successParsingKms && ValidateDistance(kms))
                                || _s.CongratsTrainingType == "Swim")
                            {
                                // Retrieve athlete first name
                                string athleteFirstname = string.Empty;
                                Match athleteNameMatch = _regexAthleteName.Match(str);
                                if (athleteNameMatch.Success)
                                {
                                    athleteFirstname = athleteNameMatch.Groups[1].Value.Split(' ')[0];
                                }

                                // Replace the [name] tag with athlete first name found
                                string messageCongrats = _s.CongratsMessage.Replace("[name]", athleteFirstname);

                                // Scroll to the comment button
                                Console.WriteLine("Found run workout to add comment...");
                                Utilities.ScrollToElement(addCommentButton);
                                System.Threading.Thread.Sleep(1000);

                                // Get the card html of the workout
                                var element2 = Utilities.GetParentElement(Utilities.GetParentElement(Utilities.GetParentElement(Utilities.GetParentElement(button))));
                                var str2 = element2.GetDomProperty("innerHTML");

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

        public static bool ValidateTrainingType(string html)
        {
            /*
             * title="Weight Training"  title="Treinamento com peso"
               title="Run"  		    title="Corrida"
               title="Hike"   		    title="Trilha"
               title="Walk"  	        title="Caminhada"
               title="Ride"  	        title="Pedalada"
               title="Swim"    	        title="Natação"
            */

            if (_s.CongratsTrainingType == "Weight Training")
            {
                return (html.Contains(@"<title>Treinamento com peso</title>") || html.Contains(@"<title>Weight Training</title>"));
            }

            if (_s.CongratsTrainingType == "Run")
            {
                return (html.Contains(@"<title>Corrida</title>") || html.Contains(@"<title>Run</title>"));
            }

            if (_s.CongratsTrainingType == "Hike")
            {
                return (html.Contains(@"<title>Trilha</title>") || html.Contains(@"<title>Hike</title>"));
            }

            if (_s.CongratsTrainingType == "Walk")
            {
                return (html.Contains(@"<title>Caminhada</title>") || html.Contains(@"<title>Walk</title>"));
            }

            if (_s.CongratsTrainingType == "Ride")
            {
                return (html.Contains(@"<title>Pedalada</title>") || html.Contains(@"<title>Ride</title>"));
            }

            if (_s.CongratsTrainingType == "Swim")
            {
                return (html.Contains(@"<title>Natação</title>") || html.Contains(@"<title>Swim</title>"));
            }

            return false;
        }

        public static bool ValidateDistance(float kms)
        {
            // No need to validate distance
            if (_s.CongratsMinimumDistance == 0 && _s.CongratsMaximumDistance == 0)
                return true;

            // Check if distance is within provided range
            if (kms >= _s.CongratsMinimumDistance && kms <= _s.CongratsMaximumDistance)
                return true;

            return false;
        }
    }
}
