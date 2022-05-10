// Application to automate like all workouts in the Strava feed and more :)

using _s = LikeAllStrava.ShraredObjects;

namespace LikeAllStrava
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Validate command-line parameters if we have them
                if (args.Length > 0)
                {
                    // Parameters can be followpeople and then URL
                    // Example of url: https://www.strava.com/athletes/9954999/follows?type=following
                    // If we don't have the url, ask the user

                    if (args.Length == 1 && args[0] == "followpeople")
                    {
                        Console.WriteLine("\r\nPlease enter Strava URL of athlete when in the \"Following\" tab:");
                        _s.UrlFollowPeople = Console.ReadLine();
                    }
                    if (args.Length == 2 && args[0] == "followpeople")
                    {
                        _s.UrlFollowPeople = args[1];
                    }

                    // Parameters can be congratscomment and then message (placeholder [name] will be first name of the athlete)
                    // Example of parameters: congratscomment "Congratulations for the long run [name]!"
                    // If we don't have the message, ask the user

                    if (args.Length == 1 && args[0] == "congratscomment")
                    {
                        Console.WriteLine("\r\nPlease enter congratulations message for the run (use [name] as first name of the athlete to be replaced):");
                        _s.MessageCongratsComment = Console.ReadLine();
                    }
                    if (args.Length == 2 && args[0] == "congratscomment")
                    {
                        _s.MessageCongratsComment = args[1];
                    }
                }

                // Read config file
                Utilities.InitializeConfig(args);

                // Ask user for Strava login data if required first time
                StravaLogin.RequestLoginData();

                // Login into Strava platform
                StravaLogin.Login();

                // Check if we need to follow more people
                if (args.Length > 0 && args[0] == "followpeople" && !string.IsNullOrEmpty(_s.UrlFollowPeople))
                {
                    // Call automation to follow more people
                    StravaFollow.FollowPeople(_s.UrlFollowPeople);
                }
                // Check if we need to congratulate the people for the run workout
                else if (args.Length > 0 && args[0] == "congratscomment" && !string.IsNullOrEmpty(_s.MessageCongratsComment))
                {
                    // Call automation to add congratulations comments for run workout
                    StravaCongrats.CongratsComment();
                }
                else 
                {
                    // Like all the workouts in the Strava newsfeed
                    StravaLike.LikeWorkouts();
                }

                Console.WriteLine("Finished! Thanks!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Automation error (maybe wrong login data?): " + ex.ToString());
            }
            finally
            {
                // Close all GeckoDriver processes and exit
                FirefoxDriverControl.CloseAllGeckoDrivers();
            }
        }
    }
}