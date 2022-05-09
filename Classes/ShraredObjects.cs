using OpenQA.Selenium;

namespace LikeAllStrava
{
    public class ShraredObjects
    {
        public static IWebDriver ChromeDriver;
        public static IJavaScriptExecutor JavascriptExecutor;
        public static StravaSettings StravaSettings = new();
        public static string UrlFollowPeople = string.Empty;
        public static string MessageCongratsComment = string.Empty;
    }
}
