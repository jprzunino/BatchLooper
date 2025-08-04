using System.Diagnostics;
using System.Globalization;

namespace BatchLooper.Core.Helpers
{
    public static class ExecutionTimeHelper
    {
        private const string msk = "{0} dia(s) {1:00}:{2:00}:{3:00}.{4:00}";

        public static string GetDuration(this Stopwatch? sw)
        {
            if (sw == null)
                return string.Format(provider: CultureInfo.InvariantCulture, format: msk, 0, 0, 0, 0, 0);

            var result = sw.Elapsed.GetDuration();

            return result;
        }

        public static string GetDuration(this TimeSpan ts)
        {
            var result = string.Format(provider: CultureInfo.InvariantCulture, format: msk, ts.Days, ts.Hours, ts.Minutes, ts.Seconds, (ts.TotalMilliseconds / 10));

            return result;
        }

        public static string GetDuration(this TimeSpan? ts)
        {
            if (ts is null)
                return string.Format(provider: CultureInfo.InvariantCulture, format: msk, 0, 0, 0, 0, 0);

            var result = ts.Value.GetDuration();

            return result;
        }

        public static string GetAverageDuration(this IEnumerable<Stopwatch> lst)
        {
            if (lst == null)
                return (null as Stopwatch).GetDuration();

            var ticksAverage = lst.Any() ? lst.Average(s => s.Elapsed.Ticks) : 0;
            var duration = TimeSpan.FromTicks(Convert.ToInt64(ticksAverage));
            var result = duration.GetDuration();

            return result;
        }

        public static string GetAverageDuration(this IEnumerable<TimeSpan> lst)
        {
            if (lst == null)
                return (null as Stopwatch).GetDuration();

            var ticksAverage = lst.Average(s => s.Ticks);
            var duration = TimeSpan.FromTicks(Convert.ToInt64(ticksAverage));
            var result = duration.GetDuration();

            return result;
        }
    }
}
