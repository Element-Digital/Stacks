using System.Globalization;

namespace Stacks.Ui;

internal static class RelativeTime
{
    public static string Format(DateTimeOffset? when, DateTimeOffset now)
    {
        if (when is null) return "—";

        var elapsed = now - when.Value;
        if (elapsed.TotalSeconds < 0) return when.Value.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (elapsed.TotalSeconds < 60) return "just now";
        if (elapsed.TotalMinutes < 60) return string.Create(CultureInfo.InvariantCulture, $"{(int)elapsed.TotalMinutes}m ago");
        if (elapsed.TotalHours < 24) return string.Create(CultureInfo.InvariantCulture, $"{(int)elapsed.TotalHours}h ago");
        if (elapsed.TotalDays < 2) return "yesterday";
        if (elapsed.TotalDays < 7) return string.Create(CultureInfo.InvariantCulture, $"{(int)elapsed.TotalDays}d ago");
        return when.Value.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
