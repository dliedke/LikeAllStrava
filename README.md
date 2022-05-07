# LikeAllStrava

Give kudos to people's workouts in Strava automatically.

In first run the application will ask for you login (email), password and complete name in Strava and save it encrypted in config file for next runs.

# Follow more people

It is possible to follow more people with automation.

Just enter parameters followpeople with the Stava URL of the athlete in the "Following" tab, example:

LikeAllStrava followpeople https://www.strava.com/athletes/9954999/follows?type=following

In case you don't provide the URL in command line the application will request it.

# Congratulations for long run

There is a feature to add comments for long runs.

Just enter parameter congratslongrun and then message (placeholder [name] will be first name of the athlete), example:

congratslongrun "Congratulations for the long run [name]!"

In case you don't provide the message in command line the application will request it.

Note: emoticons are not supported due to Selenium limitations.

# Use it wisely

Remember that if you use too much this application you will be temporary blocked from giving kudos/following new people in the Strava platform.

There is an limit of about 50 likes/per hour. Use it wisely.

# Requirements

<a href="https://dotnet.microsoft.com/en-us/download">.NET 6.0</a> is required to run this application.
