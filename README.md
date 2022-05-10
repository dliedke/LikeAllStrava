# LikeAllStrava

Give kudos to people's workouts in Strava automatically.

In first run the application will ask for you login (email), password and complete name in Strava and save it encrypted in config file for next runs.

# Follow more people

It is possible to follow more people with automation.

Just enter parameters followpeople with the Stava URL of the athlete in the "Following" tab, example:

LikeAllStrava followpeople https://www.strava.com/athletes/9954999/follows?type=following

In case you don't provide the URL in command line the application will request it.

Note: You must be following athete already for this feature to work.

# Congratulations comment

There is a feature to add congratulation comments for long runs / run races.

Just enter parameter congratscomment and then message (placeholder [name] will be first name of the athlete), examples:

congratscomment "Congratulations for the long run [name] 🏃‍♂️😀💪"

or 

congratscomment "Congratulations for the run race [name] 🏃‍♂️😀💪"

In case you don't provide the message in command line the application will request it.

Note: Congrats message support emoticons because application uses copy+paste clipboard for automation.
      So your clipboard existing content will be lost when running this application.

# Use it wisely

Remember that if you use too much this application you will be temporary blocked from giving kudos/following new people in the Strava platform.

There is an limit of about 50 likes/per hour. Use it wisely.

# Requirements

<a href="https://dotnet.microsoft.com/en-us/download">.NET 6.0</a> is required to run this application.
