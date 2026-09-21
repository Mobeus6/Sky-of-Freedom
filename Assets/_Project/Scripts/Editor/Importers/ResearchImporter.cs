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

            var ids = new HashSet<string>();
            foreach (CsvRow row in rows)
            {
                string id = row["ID"].Trim();
                if (id.Length == 0) continue;
                if (!ids.Add(id)) throw new Exception($"Duplicate research ID: {id}");
                ParseBranch(row["Branch"]);
                ParseCategory(row["Category"]);
                ReadInteger(row, "Tier", 1, ParseCategory(row["Category"]) == ResearchCategory.AI ? 3 : 5);
                ReadInteger(row, "Cost", 0, int.MaxValue);
                ParseTime(row["Time"]);
                foreach (string column in BonusColumns)
                    ReadBonus(row, column, 0f);
            }

            foreach (CsvRow row in rows)
            {
                string id = row["ID"].Trim();
                if (id.Length == 0) continue;
                foreach (string raw in row["Dependencies"].Split(';'))
                {
                    string dependency = raw.Trim();
                    if (dependency.Length == 0) continue;
                    if (dependency == id || (!ids.Contains(dependency) &&
                        AssetUtility.LoadByID<ResearchSO>(outputFolder, dependency) == null))
                        throw new Exception($"{id}: invalid dependency {dependency}");
                }
            }

            int created = 0;
            int updated = 0;

            foreach (CsvRow row in rows)
            {
                string id = row["ID"].Trim();

                if (string.IsNullOrWhiteSpace(id))
                    continue;

                ResearchSO research =
                    AssetUtility.LoadByID<ResearchSO>(outputFolder, id);
                bool isNew = research == null;

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

                int tier = Mathf.Clamp(row.GetInt("Tier"), 1, 5);
                bool isAI = ParseCategory(row["Category"]) == ResearchCategory.AI;
                if (isAI) tier = Mathf.Min(tier, 3);
                int factoryLevel = isAI ? tier + 2 : tier;
                if (row.GetInt("Factory Lv.") != factoryLevel)
                    Debug.LogWarning($"{id}: Factory Lv. normalized to {factoryLevel} (five-level progression).");

                string effect = row["Effect"];
                if (id.StartsWith("RES-AI-", StringComparison.Ordinal) &&
                    (effect == "+10% Programming Speed" || effect == "+10% Factory Efficiency" ||
                     effect == "+15% Global Efficiency"))
                    effect = tier == 1 ? "+10% Research Speed" : tier == 2
                        ? "+10% Production and Assembly Speed"
                        : "+15% Research, Production and Assembly Speed";
                else if (id.StartsWith("RES-AUT-", StringComparison.Ordinal) &&
                    effect == "+5% Global Production Efficiency")
                    effect = "+5% Production and Assembly Speed";

                research.SetData(
                    id,
                    row["Name"],
                    ParseBranch(row["Branch"]),
                    ParseCategory(row["Category"]),
                   
                    tier,
                    factoryLevel,
                    row.GetInt("Cost"),
                    ParseTime(row["Time"]),
                    research.Prerequisites,
                    effect,
                    row["Unlock"]);

                var serialized = new SerializedObject(research);
                for (int i = 0; i < BonusColumns.Length; i++)
                {
                    SerializedProperty property = serialized.FindProperty(BonusFields[i]);
                    float fallback = isNew ? DefaultBonus(id, i) : i == 3 ? property.intValue : property.floatValue;
                    float value = ReadBonus(row, BonusColumns[i], fallback);
                    if (i == 3) property.intValue = (int)value;
                    else property.floatValue = value;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(research);
            }

            // Resolve after all IDs exist, including prerequisites later in the CSV.
            foreach (CsvRow row in rows)
            {
                string id = row["ID"].Trim();
                if (id.Length == 0) continue;
                ResearchSO research = AssetUtility.LoadByID<ResearchSO>(outputFolder, id);
                var serialized = new SerializedObject(research);
                SerializedProperty dependencies = serialized.FindProperty("prerequisites");
                ResearchSO[] prerequisites = ReadPrerequisites(row["Dependencies"], outputFolder);
                dependencies.arraySize = prerequisites.Length;
                for (int i = 0; i < prerequisites.Length; i++)
                    dependencies.GetArrayElementAtIndex(i).objectReferenceValue = prerequisites[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(research);
            }
            AssetUtility.Save();

            Debug.Log(
                $"Research import completed.\nCreated: {created}\nUpdated: {updated}");
        }

        private static readonly string[] BonusColumns =
        {
            "Production Speed Bonus Percent", "Assembly Speed Bonus Percent",
            "Research Speed Bonus Percent", "Storage Capacity Bonus", "Material Discount Percent"
        };

        private static readonly string[] BonusFields =
        {
            "productionSpeedBonusPercent", "assemblySpeedBonusPercent",
            "researchSpeedBonusPercent", "storageCapacityBonus", "materialDiscountPercent"
        };

        private static float ReadBonus(CsvRow row, string column, float fallback)
        {
            string raw = row[column].Trim();
            if (raw.Length == 0) return fallback;
            if (!float.TryParse(raw, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float value) ||
                float.IsNaN(value) || float.IsInfinity(value) || value < 0f ||
                (column == "Material Discount Percent" && value > 100f) ||
                (column == "Storage Capacity Bonus" && (value >= int.MaxValue || value != Math.Truncate(value))))
                throw new Exception($"{row["ID"]}: invalid {column}: {raw}");
            return value;
        }

        private static float DefaultBonus(string id, int field)
        {
            if (id.StartsWith("RES-PRD-", StringComparison.Ordinal) && field == 0) return 10f;
            if (id.StartsWith("RES-ASM-", StringComparison.Ordinal) && field == 1) return 10f;
            if (id.StartsWith("RES-AUT-", StringComparison.Ordinal) && field < 2) return 5f;
            if (id.StartsWith("RES-STO-", StringComparison.Ordinal) && field == 3) return 20f;
            if (id.StartsWith("RES-SUP-", StringComparison.Ordinal) && field == 4) return 3f;
            if (id == "RES-AI-01" && field == 2) return 10f;
            if (id == "RES-AI-02" && field < 2) return 10f;
            if (id == "RES-AI-03" && field < 3) return 15f;
            return 0f;
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
            string input = (value ?? string.Empty).Trim();
            var match = System.Text.RegularExpressions.Regex.Match(input,
                @"^(\d+(?:\.\d+)?)\s*(s|sec|secs|second|seconds|m|min|mins|minute|minutes|h|hr|hrs|hour|hours)?$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase |
                System.Text.RegularExpressions.RegexOptions.CultureInvariant);
            if (!match.Success || !float.TryParse(match.Groups[1].Value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float number))
                throw new FormatException($"Invalid research time: '{value}'. Use seconds, '15 min' or '1 h'.");
            string unit = match.Groups[2].Value.ToLowerInvariant();
            float seconds = number * (unit.StartsWith("h") ? 3600f : unit.StartsWith("m") ? 60f : 1f);
            if (float.IsNaN(seconds) || float.IsInfinity(seconds))
                throw new FormatException($"Research time is too large: '{value}'.");
            return seconds;
        }

        private static int ReadInteger(CsvRow row, string column, int minimum, int maximum)
        {
            if (!int.TryParse(row[column].Trim(), System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out int value) ||
                value < minimum || value > maximum)
                throw new FormatException($"{row["ID"]}: invalid {column}: '{row[column]}'.");
            return value;
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
