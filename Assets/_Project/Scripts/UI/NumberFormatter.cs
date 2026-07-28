using System;

namespace SkyOfFreedom.Utilities
{
    public static class NumberFormatter
    {
        public static string Format(long value)
        {
            if (value >= 1_000_000_000_000_000_000)
                return (value / 1_000_000_000_000_000_000d).ToString("0.#") + "E";

            if (value >= 1_000_000_000_000_000)
                return (value / 1_000_000_000_000_000d).ToString("0.#") + "P";

            if (value >= 1_000_000_000_000)
                return (value / 1_000_000_000_000d).ToString("0.#") + "T";

            if (value >= 1_000_000_000)
                return (value / 1_000_000_000d).ToString("0.#") + "B";

            if (value >= 1_000_000)
                return (value / 1_000_000d).ToString("0.#") + "M";

            if (value >= 1_000)
                return (value / 1_000d).ToString("0.#") + "K";

            return value.ToString("N0");
        }
    }
}