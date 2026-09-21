using System;
using System.Globalization;

namespace SkyOfFreedom.Services
{
    public static class OfflineProgress
    {
        public static double Elapsed(string timestamp, DateTime now)
        {
            return DateTime.TryParse(timestamp, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out DateTime saved)
                ? Math.Max(0d, (now.ToUniversalTime() - saved.ToUniversalTime()).TotalSeconds)
                : 0d;
        }

        // Split at research completion, so its new bonuses never apply retroactively.
        // Both elapsed intervals end at the same instant, even for legacy saves.
        public static void Advance(double productionSeconds, double researchSeconds,
            double researchRemainingSeconds, Action<double> production, Action<double> research)
        {
            productionSeconds = Sanitize(productionSeconds);
            researchSeconds = Sanitize(researchSeconds);
            double afterCompletion = !double.IsNaN(researchRemainingSeconds) &&
                researchRemainingSeconds >= 0d && researchRemainingSeconds <= researchSeconds
                ? Math.Min(productionSeconds, researchSeconds - researchRemainingSeconds) : 0d;
            double beforeCompletion = productionSeconds - afterCompletion;
            if (beforeCompletion > 0d) production(beforeCompletion);
            research(researchSeconds);
            if (afterCompletion > 0d) production(afterCompletion);
        }

        private static double Sanitize(double seconds)
        {
            return double.IsNaN(seconds) || double.IsInfinity(seconds) ? 0d : Math.Max(0d, seconds);
        }
    }
}
