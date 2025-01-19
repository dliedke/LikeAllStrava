using OpenQA.Selenium;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using _s = LikeAllStrava.ShraredObjects;

namespace LikeAllStrava
{
    public class Utilities
    {
        public static void InitializeConfig(string[] args)
        {
            // Load configuration file with full name for Strava
            IConfiguration config = new ConfigurationBuilder()
                                       .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                                       .AddEnvironmentVariables()
                                       .AddCommandLine(args)
                                       .Build();

            _s.StravaSettings = config.GetRequiredSection("StravaSettings").Get<StravaSettings>();
        }

        public static void SaveConfigFile(Settings settings)
        {
            // Write idented json file
            var jsonWriteOptions = new JsonSerializerOptions()
            {
                WriteIndented = true
            };
            jsonWriteOptions.Converters.Add(new JsonStringEnumConverter());

            // Serialize settings with Strava full name
            // and save in appsettings.json in the application path
            var newJson = JsonSerializer.Serialize(settings, jsonWriteOptions);
            var appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            File.WriteAllText(appSettingsPath, newJson);
        }

        public static void ClickElementJavascript(IWebElement element)
        {
            _s.JavascriptExecutor.ExecuteScript("var evt = document.createEvent('MouseEvents');" + "evt.initMouseEvent('click',true, true, window, 0, 0, 0, 0, 0, false, false, false, false, 0,null);" + "arguments[0].dispatchEvent(evt);", element);
        }

        public static IWebElement GetParentElement(IWebElement e)
        {
            // Retrieve the parent DOM element from the HTML
            return e.FindElement(By.XPath(".."));
        }

        public static void ScrollToElement(IWebElement element)
        {
            try
            {
                if (element.Location.Y > 200)
                {
                    // Scroll page until the element but showing the workout pictures
                    _s.JavascriptExecutor.ExecuteScript($"window.scrollTo({0}, {element.Location.Y - 600 })");
                }
            }
            catch { }
        }
    }
}
