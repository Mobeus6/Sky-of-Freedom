using System;
using System.Collections.Generic;
using SkyOfFreedom.Data;
using UnityEditor;
using UnityEngine;

namespace SkyOfFreedom.Editor
{
    public static class ResearchImporter
    {
        public static void Import(string csvPath, string outputFolder)
        {
            List<CsvRow> rows = CsvReader.Read(csvPath);

            int created = 0;
            int updated = 0;

            foreach (CsvRow row in rows)
            {
                string id = row["ID"];

                if (string.IsNullOrWhiteSpace(id))
                    continue;

                ResearchSO research =
                    AssetUtility.LoadByID<ResearchSO>(outputFolder, id);

                if (research == null)
                {
                    research = AssetUtility.CreateAsset<ResearchSO>(
                        outputFolder,
                        id);

                    created++;
                }
                else
                {
                    updated++;
                }

                ResearchSO[] prerequisites =
                    ReadPrerequisites(row["Dependencies"], outputFolder);

                research.SetData(
                    id,
                    row["Name"],
                    ParseBranch(row["Branch"]),
                    ParseCategory(row["Category"]),
                    row.GetInt("Tier"),
                    row.GetInt("Factory Lv."),
                    row.GetInt("Cost"),
                    ParseTime(row["Time"]),
                    prerequisites,
                    row["Effect"],
                    row["Unlock"]);

                EditorUtility.SetDirty(research);
            }

            AssetUtility.Save();

            Debug.Log(
                $"Research import completed.\nCreated: {created}\nUpdated: {updated}");
        }

        private static ResearchSO[] ReadPrerequisites(
            string value,
            string outputFolder)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<ResearchSO>();

            string[] ids = value.Split(';');

            List<ResearchSO> result = new();

            foreach (string raw in ids)
            {
                string id = raw.Trim();

                if (string.IsNullOrWhiteSpace(id))
                    continue;

                ResearchSO prerequisite =
                    AssetUtility.LoadByID<ResearchSO>(
                        outputFolder,
                        id);

                if (prerequisite == null)
                {
                    Debug.LogWarning(
                        $"Research prerequisite not found: {id}");
                    continue;
                }

                result.Add(prerequisite);
            }

            return result.ToArray();
        }
        private static float ParseTime(string value)
        {
            value = value.Trim().ToLower();

            if (string.IsNullOrEmpty(value))
                return 0f;

            if (float.TryParse(
                    value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out float number))
            {
                return number;
            }

            if (value.EndsWith("s"))
            {
                value = value.Replace("sec", "")
                             .Replace("secs", "")
                             .Replace("second", "")
                             .Replace("seconds", "")
                             .Replace("s", "")
                             .Trim();

                if (float.TryParse(value, out number))
                    return number;
            }

            if (value.EndsWith("min"))
            {
                value = value.Replace("minutes", "")
                             .Replace("minute", "")
                             .Replace("mins", "")
                             .Replace("min", "")
                             .Trim();

                if (float.TryParse(value, out number))
                    return number * 60f;
            }

            Debug.LogWarning($"Unknown time format: {value}");

            return 0f;
        }
        private static ResearchBranch ParseBranch(string value)
        {
            value = value.Trim();

            switch (value)
            {
                case "Production":
                    return ResearchBranch.Production;

                case "Business":
                    return ResearchBranch.Business;

                case "Advanced":
                    return ResearchBranch.Advanced;

                case "Endgame":
                    return ResearchBranch.Endgame;

                // Якщо у CSV випадково записана категорія замість гілки
                case "Assembly":
                case "Programming":
                case "Storage":
                case "Industrial Automation":
                case "Production Line":
                    return ResearchBranch.Production;

                case "Marketing":
                case "Finance":
                case "Supply Chain":
                case "Government Relations":
                    return ResearchBranch.Business;

                case "AI":
                case "AI Systems":
                case "Corporate Management":
                case "Advanced Logistics":
                    return ResearchBranch.Advanced;

                case "Factory AI":
                case "Autonomous Factory":
                case "Next Generation Manufacturing":
                    return ResearchBranch.Endgame;

                default:
                    throw new Exception($"Unknown ResearchBranch: {value}");
            }
        }

        private static ResearchCategory ParseCategory(string value)
        {
            value = value.Trim();

            switch (value)
            {
                case "Assembly":
                    return ResearchCategory.Assembly;

                case "Production":
                case "Production Line":
                    return ResearchCategory.Production;

                case "Programming":
                    return ResearchCategory.Programming;

                case "Storage":
                    return ResearchCategory.Storage;

                case "Industrial Automation":
                    return ResearchCategory.IndustrialAutomation;

                case "Marketing":
                    return ResearchCategory.Marketing;

                case "Finance":
                    return ResearchCategory.Finance;

                case "Supply Chain":
                    return ResearchCategory.SupplyChain;

                case "Government Relations":
                    return ResearchCategory.GovernmentRelations;

                case "AI":
                case "AI Systems":
                    return ResearchCategory.AI;

                case "Corporate Management":
                    return ResearchCategory.CorporateManagement;

                case "Advanced Logistics":
                    return ResearchCategory.AdvancedLogistics;

                case "Factory AI":
                case "Autonomous Factory":
                case "Next Generation Manufacturing":
                case "Endgame":
                    return ResearchCategory.Endgame;

                default:
                    throw new Exception($"Unknown ResearchCategory: {value}");
            }
        }
    }
}