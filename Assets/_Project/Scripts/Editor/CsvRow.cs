using System.Collections.Generic;

namespace SkyOfFreedom.Editor
{
    public class CsvRow
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>();

        public IReadOnlyDictionary<string, string> Values => values;

        public void Add(string key, string value)
        {
            values[key] = value;
        }

        public string Get(string key)
        {
            if (values.TryGetValue(key, out string value))
                return value;

            return string.Empty;
        }

        public int GetInt(string key)
        {
            int.TryParse(Get(key), out int value);
            return value;
        }

        public float GetFloat(string key)
        {
            float.TryParse(
                Get(key),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out float value);

            return value;
        }

        public bool Has(string key)
        {
            return values.ContainsKey(key);
        }

        public string this[string key]
        {
            get { return Get(key); }
        }
    }
}