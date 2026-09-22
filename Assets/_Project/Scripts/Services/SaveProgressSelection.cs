using System;
using System.Collections.Generic;
using System.Globalization;
using SkyOfFreedom.Data;

namespace SkyOfFreedom.Services
{
    public static class SaveProgressSelection
    {
        public static bool PreferLocal(PlayerData local, PlayerData cloud)
        {
            if (local == null) return false;
            if (cloud == null) return true;
            long[] left = Progress(local);
            long[] right = Progress(cloud);
            for (int i = 0; i < left.Length; i++)
                if (left[i] != right[i]) return left[i] > right[i];
            // Equal or malformed timestamps prefer the server; never depend on list ordering.
            return Timestamp(local.Account?.LastSaveAtUtc) > Timestamp(cloud.Account?.LastSaveAtUtc);
        }

        private static long[] Progress(PlayerData data)
        {
            var factory = data.Factory;
            var research = new HashSet<string>(StringComparer.Ordinal);
            if (data.Research?.ResearchStates != null)
                foreach (var state in data.Research.ResearchStates)
                    if (state != null && state.IsCompleted && !string.IsNullOrEmpty(state.ResearchId))
                        research.Add(state.ResearchId);
            var licenses = new HashSet<string>(StringComparer.Ordinal);
            if (data.Licenses?.UnlockedLicenseIds != null)
                foreach (string id in data.Licenses.UnlockedLicenseIds)
                    if (!string.IsNullOrEmpty(id)) licenses.Add(id);
            return new long[]
            {
                Level(factory?.FactoryLevel ?? 1),
                Level(factory?.WarehouseLevel ?? 1) + Level(factory?.ProductionLevel ?? 1) +
                Level(factory?.AssemblyLevel ?? 1) + Level(factory?.ResearchLevel ?? 1),
                research.Count, licenses.Count
            };
        }

        private static int Level(int value) { return Math.Max(1, Math.Min(5, value)); }
        private static DateTime Timestamp(string value)
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime result)
                ? result : DateTime.MinValue;
        }
    }
}
