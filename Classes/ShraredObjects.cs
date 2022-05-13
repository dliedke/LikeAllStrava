using OpenQA.Selenium;

namespace LikeAllStrava
{
    public class ShraredObjects
    {
        public static IWebDriver ChromeDriver;
        public static IJavaScriptExecutor JavascriptExecutor;
        public static StravaSettings StravaSettings = new();
        public static string FullName = string.Empty;
        public static string UrlFollowPeople = string.Empty;
        public static string CongratsMessage = string.Empty;
        public static string CongratsTrainingType = string.Empty;
        public static int CongratsMinimumDistance = -1;
        public static int CongratsMaximumDistance = 0;
    }
}
