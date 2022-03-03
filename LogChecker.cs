public class LogChecker
{
    public enum Level
    {
        Disabled = 0,
        Important = 1,
        Normal = 2,
        Verbose = 3
    }

    public static Level GlobalLevel = Level.Disabled;
    public Level CheckerLevel = Level.Disabled;

    public LogChecker(Level level)
    {
        CheckerLevel = level;
    }

    private bool IsAtLeast(Level level)
    {
        // global level check
        if (level > GlobalLevel)
            return false;

        // current checker level check
        return level <= CheckerLevel;
    }

    public bool Important()
    {
        return IsAtLeast(Level.Important);
    }

    public bool Normal()
    {
        return IsAtLeast(Level.Normal);
    }

    public bool Verbose()
    {
        return IsAtLeast(Level.Verbose);
    }
}