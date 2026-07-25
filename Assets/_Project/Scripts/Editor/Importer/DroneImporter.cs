using SkyOfFreedom.Data;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Codice.Client.Commands.WkTree.WorkspaceTreeNode;

namespace SkyOfFreedom.Editor
{
    public static class DroneImporter
    {
        private const string DroneFolder = "Assets/Data/Drones";
        private const string ComponentFolder = "Assets/_Project/Data/ScriptableObjects/Components";


        public static void Import(string csvPath, string outputFolder)
        {
            List<CsvRow> rows = CsvReader.Read(csvPath);

            int created = 0;
            int updated = 0;

            foreach (CsvRow row in rows)
            {
                string id = row["ID"];

                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }
                Debug.Log(ComponentFolder);
                DroneModelSO model =
                    AssetUtility.LoadByID<DroneModelSO>(outputFolder, id);

                if (model == null)
                {
                    model = AssetUtility.CreateAsset<DroneModelSO>(
                        outputFolder,
                        id);

                    created++;
                }
                else
                {
                    updated++;
                }

                List<DroneComponent> components =
                    ReadComponents(row);

                model.SetData(
                    id,
                    row["Model"],
                    row["Description"],
                    ParsePlatform(row["Platform"]),
                    ParseType(row["Type"]),
                    ParseTier(row["Tier"]),
                    row.GetFloat("Assembly Time"),
                    components,
                    row.GetInt("Flight Distance"),
                    row.GetInt("Payload Capacity"),
                    row.GetInt("Durability"),
                    row.GetInt("Navigation"),
                    row.GetInt("Stealth"));

                EditorUtility.SetDirty(model);
            }

            AssetUtility.Save();

            Debug.Log(
                $"Drone import completed.\nCreated: {created}\nUpdated: {updated}");
        }

        private static List<DroneComponent> ReadComponents(CsvRow row)
        {
            List<DroneComponent> result = new();

            foreach (var pair in row.Values)
            {
                string componentId = pair.Key;

                if (!componentId.StartsWith("CMP-"))
                    continue;

                if (!int.TryParse(pair.Value, out int amount))
                    continue;

                if (amount <= 0)
                    continue;

                ComponentSO component =
                    AssetUtility.LoadByID<ComponentSO>(
                        ComponentFolder,
                        componentId);

                if (component == null)
                {
                    Debug.LogWarning($"Component not found: {componentId}");
                    continue;
                }

                result.Add(new DroneComponent(component, amount));
            }

            return result;
        }
        

        private static DronePlatform ParsePlatform(string value)
        {
            Debug.Log($"Platform: [{value}]");

            if (!System.Enum.TryParse(value.Trim(), true, out DronePlatform result))
            {
                throw new System.Exception($"Unknown Platform: [{value}]");
            }

            return result;           
        }

        private static DroneType ParseType(string value)
        {
            value = value.Trim();

            switch (value)
            {
                case "Recon":
                    return DroneType.Recon;

                case "Attack":
                    return DroneType.Attack;

                case "Anti-Air":
                case "AntiAir":
                    return DroneType.AntiAir;

                case "Electronic Warfare":
                case "ElectronicWarfare":
                    return DroneType.ElectronicWarfare;

                case "Heavy Attack":
                case "HeavyAttack":
                    return DroneType.HeavyAttack;

                default:
                    throw new System.Exception($"Unknown DroneType: {value}");
            }
        }

        private static DroneTier ParseTier(string value)
        {
            Debug.Log("Tier = " + value);

            return (DroneTier)System.Enum.Parse(
                typeof(DroneTier),
                value.Trim(),
                true);
        }
    }
}
        