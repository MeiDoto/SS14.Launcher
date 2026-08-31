using System;
using System.Globalization;
using SS14.Launcher.Localization;

namespace SS14.Launcher.Utility;

public static class PlaytimeFormatter
{
    public static string Format(long totalSeconds)
    {
        var culture = LocalizationManager.Instance.CurrentCulture ?? CultureInfo.CurrentCulture;
        var isRu = culture.TwoLetterISOLanguageName.Equals("ru", StringComparison.OrdinalIgnoreCase);
        return FormatPlaytime(totalSeconds, isRu);
    }

    public static string FormatPlaytime(long totalSeconds, bool isRussian)
    {
        if (totalSeconds <= 0)
            return isRussian ? "0 минут" : "0 minutes";

        var hours = totalSeconds / 3600;
        var mins = (totalSeconds % 3600) / 60;

        if (hours > 0)
        {
            if (isRussian)
            {
                var hourStr = PluralizeRu(hours, "час", "часа", "часов");
                if (mins > 0)
                {
                    var minStr = PluralizeRu(mins, "минута", "минуты", "минут");
                    return $"{hours} {hourStr} {mins} {minStr}";
                }
                return $"{hours} {hourStr}";
            }
            else
            {
                var hourStr = hours == 1 ? "hour" : "hours";
                if (mins > 0)
                {
                    var minStr = mins == 1 ? "minute" : "minutes";
                    return $"{hours} {hourStr} {mins} {minStr}";
                }
                return $"{hours} {hourStr}";
            }
        }
        else
        {
            mins = Math.Max(1, mins);
            if (isRussian)
            {
                var minStr = PluralizeRu(mins, "минута", "минуты", "минут");
                return $"{mins} {minStr}";
            }
            else
            {
                var minStr = mins == 1 ? "minute" : "minutes";
                return $"{mins} {minStr}";
            }
        }
    }

    private static string PluralizeRu(long n, string one, string twoToFour, string fiveAndMore)
    {
        var abs = Math.Abs(n) % 100;
        var rem = abs % 10;
        if (abs is >= 11 and <= 19)
            return fiveAndMore;
        if (rem == 1)
            return one;
        if (rem is >= 2 and <= 4)
            return twoToFour;
        return fiveAndMore;
    }
}
