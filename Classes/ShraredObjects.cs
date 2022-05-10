using OpenQA.Selenium;

namespace LikeAllStrava
{
    public class ShraredObjects
    {
        public static IWebDriver FirefoxDriver;
        public static IJavaScriptExecutor JavascriptExecutor;
        public static StravaSettings StravaSettings = new();
        public static string FullName = string.Empty;
        public static string UrlFollowPeople = string.Empty;
        public static string MessageCongratsComment = string.Empty;
    }
}
