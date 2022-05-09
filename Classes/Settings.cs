namespace LikeAllStrava
{
    public class Settings
    {
        public StravaSettings? StravaSettings { get; set; }
    }

    public class StravaSettings
    {
        public string? Login { get; set; }
        public string? Password { get; set; }
        public string? FullName { get; set; }
    }
}
