// Application to automate like all workouts in the Strava feed and more :)

using System.Text.RegularExpressions;
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

                    // Parameters can be congratscomment "[training type]" [minimum distance-maximum distance] "[message]"
                    //  (placeholder [name] will be replaced with first name of the athlete)
                    //
                    // Example of parameters: congratscomment "run" 10-100 "Congratulations for the long run [name]!"
                    // If minimum distance-maximum distance is 0-0 all workouts of the type provided will be considered for comments

                    Regex regExValidDistanceRange = new Regex(@"^[\d]{1,3}-[\d]{1,3}$");

                    // If we don't have the parameters, ask the user
                    if (args.Length <= 3 && args[0] == "congratscomment")
                    {
                        // Asks for training type
                        while (string.IsNullOrEmpty(_s.CongratsTrainingType))
                        {
                            Console.WriteLine(@"Please enter training type for congratulations message (1-Run,2-Ride,3-Walk,4-Swim,5-Hike,6-Weight Training):");
                            string? trainingType = Console.ReadLine();
                            if (trainingType == "1") _s.CongratsTrainingType = "Run";
                            if (trainingType == "2") _s.CongratsTrainingType = "Ride";
                            if (trainingType == "3") _s.CongratsTrainingType = "Walk";
                            if (trainingType == "4") _s.CongratsTrainingType = "Swim";
                            if (trainingType == "5") _s.CongratsTrainingType = "Hike";
                            if (trainingType == "6") _s.CongratsTrainingType = "Weight Training";
                        }

                        // Asks for distance range
                        while (_s.CongratsMinimumDistance == -1)
                        {
                            Console.WriteLine("\r\nPlease enter minimum and maximum distance for the workout (ex: 10-21):");
                            string? minMaxDistance = Console.ReadLine();
                            if (minMaxDistance!=null && regExValidDistanceRange.IsMatch(minMaxDistance))
                            {
                                _s.CongratsMinimumDistance = Convert.ToInt32(minMaxDistance.Split('-')[0]);
                                _s.CongratsMaximumDistance = Convert.ToInt32(minMaxDistance.Split('-')[1]);
                            }
                        }

                        // Asks for congrats message
                        while (string.IsNullOrEmpty(_s.CongratsMessage))
                        {
                            Console.WriteLine("\r\nPlease enter congratulations message for the run (use [name] as first name of the athlete to be replaced):");
                            _s.CongratsMessage = Console.ReadLine();
                        }
                    }

                    // All parameters were provided
                    if (args.Length == 4 && args[0] == "congratscomment")
                    {
                        // Validate training type
                        Regex regExTrainingTypes = new("Run|Ride|Walk|Swim|Hike|Weight Training");
                        if (!regExTrainingTypes.IsMatch(args[1]))
                        {
                            Console.WriteLine("Invalid distance range provided.");
                            return;
                        }

                        // Validate distance range
                        if (!regExValidDistanceRange.IsMatch(args[2]))
                        {
                            Console.WriteLine("Invalid distance range provided.");
                            return;
                        }
                        
                        // Save all the parameters
                        _s.CongratsTrainingType = args[1];
                        _s.CongratsMinimumDistance = Convert.ToInt32(args[2].Split('-')[0]);
                        _s.CongratsMaximumDistance = Convert.ToInt32(args[2].Split('-')[1]);
                        _s.CongratsMessage = args[3];
                    }
                }

                // Read config file
                Utilities.InitializeConfig(args);

                // Ask user for Strava login data if required first time
                StravaLoad.RequestFullName();

                // Login into Strava platform
                StravaLoad.Load();

                // Check if we need to follow more people
                if (args.Length > 0 && args[0] == "followpeople" && !string.IsNullOrEmpty(_s.UrlFollowPeople))
                {
                    // Call automation to follow more people
                    StravaFollow.FollowPeople(_s.UrlFollowPeople);
                }
                // Check if we need to congratulate the people for the run workout
                else if (args.Length > 0 && args[0] == "congratscomment" && !string.IsNullOrEmpty(_s.CongratsMessage))
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
                // Close all EdgeDriver processes and exit
                EdgeDriverControl.CloseAllEdgeDrivers();
            }
        }
    }
}