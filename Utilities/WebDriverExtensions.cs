// This class is used to wait until a web element is available 
// using a configurable timeout to wait

using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace LikeAllStrava
{
    public static class WebDriverExtensions
    {
        public class WaitExtension
        {
            public static IWebElement WaitUntilElement(IWebDriver driver, By elementLocator, int timeout = 60)
            {
                WaitUntilElementExists(driver, elementLocator, timeout);
                return WaitUntilElementIsClicable(driver, elementLocator, timeout);
            }

            private static IWebElement WaitUntilElementExists(IWebDriver driver, By elementLocator, int timeout = 60)
            {
                try
                {
                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeout));
                    return wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists(elementLocator));
                }
                catch (NoSuchElementException)
                {
                    Console.WriteLine("Element with locator: '" + elementLocator + "' was not found in current context page.");
                    throw;
                }
            }

            private static IWebElement WaitUntilElementIsClicable(IWebDriver driver, By elementLocator, int timeout = 60)
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeout));
                return wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(elementLocator));
            }
        }
    }
}