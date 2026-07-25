using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SkyOfFreedom.Editor
{
    public static class CsvReader
    {
        public static List<CsvRow> Read(string filePath)
        {
            List<CsvRow> rows = new List<CsvRow>();

            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);

            if (lines.Length < 2)
            {
                return rows;
            }

            string[] headers = SplitLine(lines[0]);

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                string[] values = SplitLine(lines[i]);

                CsvRow row = new CsvRow();

                for (int j = 0; j < headers.Length; j++)
                {
                    string value = "";

                    if (j < values.Length)
                    {
                        value = values[j].Trim();
                    }

                    row.Add(headers[j].Trim(), value);
                }

                rows.Add(row);
            }

            return rows;
        }

        private static string[] SplitLine(string line)
        {
            List<string> values = new List<string>();

            StringBuilder current = new StringBuilder();

            bool insideQuotes = false;

            foreach (char c in line)
            {
                if (c == '"')
                {
                    insideQuotes = !insideQuotes;
                    continue;
                }

                if (c == ',' && !insideQuotes)
                {
                    values.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(c);
            }

            values.Add(current.ToString());

            return values.ToArray();
        }
    }
}