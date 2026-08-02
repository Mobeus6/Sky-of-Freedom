using SkyOfFreedom.Data;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace SkyOfFreedom.Editor
{
    public static class ComponentImporter
    {
        
        private const string MaterialFolder = "Assets/_Project/Data/ScriptableObjects/Materials";

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

                ComponentSO component =
                    AssetUtility.LoadByID<ComponentSO>(outputFolder, id);

                if (component == null)
                {
                    component = AssetUtility.CreateAsset<ComponentSO>(
                        outputFolder,
                        id);

                    created++;
                }
                else
                {
                    updated++;
                }

                List<MaterialAmount> recipe = new();

                AddMaterial(recipe, "MAT-PLASTIC", row.GetInt("PLASTIC"));
                AddMaterial(recipe, "MAT-ALUMINUM", row.GetInt("ALUMINUM"));
                AddMaterial(recipe, "MAT-CARBON", row.GetInt("CARBON"));
                AddMaterial(recipe, "MAT-BATTERY-CELL", row.GetInt("BATTERY CELL"));
                AddMaterial(recipe, "MAT-COPPER", row.GetInt("COPPER"));
                AddMaterial(recipe, "MAT-SILICONE", row.GetInt("SILICONE"));
                AddMaterial(recipe, "MAT-PCB", row.GetInt("PCB"));
                AddMaterial(recipe, "MAT-GLASS", row.GetInt("GLASS"));
                AddMaterial(recipe, "MAT-MICROCHIP", row.GetInt("MICROCHIP"));
                AddMaterial(recipe, "MAT-STEEL", row.GetInt("STEEL"));
                AddMaterial(recipe, "MAT-MAGNET", row.GetInt("MAGNET"));
                int tier = ParseTier(id);
                ComponentCategory category = ParseCategory(id);
                component.SetData(
    id,
    row["Component Name"],
    row["Description"],
    category,
    tier,
    row.GetFloat("Production Time"),
    recipe);
            }

            AssetUtility.Save();

            Debug.Log(
                $"Component import completed.\nCreated: {created}\nUpdated: {updated}");
        }

        private static void AddMaterial(
            List<MaterialAmount> recipe,
            string materialId,
            int amount)
        {
            if (amount <= 0)
                return;

            MaterialSO material =
                AssetUtility.LoadByID<MaterialSO>(
                    MaterialFolder,
                    materialId);

            if (material == null)
            {
                Debug.LogWarning($"Material not found: {materialId}");
                return;
            }

            recipe.Add(new MaterialAmount(material, amount));
        }
        private static ComponentCategory ParseCategory(string id)
        {
            if (id.Contains("HULL"))
                return ComponentCategory.Hulls;

            if (id.Contains("BATTERY"))
                return ComponentCategory.Batteries;

            if (id.Contains("CONTROLLER"))
                return ComponentCategory.Controllers;

            if (id.Contains("GPS"))
                return ComponentCategory.GPS;

            if (id.Contains("CAMERA"))
                return ComponentCategory.Cameras;

            if (id.Contains("ANTENNA"))
                return ComponentCategory.Antennas;

            if (id.Contains("SENSOR"))
                return ComponentCategory.Sensors;

            if (id.Contains("PROPELLER"))
                return ComponentCategory.Propellers;

            if (id.Contains("MOTOR"))
                return ComponentCategory.Motors;

            throw new System.Exception($"Cannot determine category from ID: {id}");
        }

        private static int ParseTier(string id)
        {
            if (id.Contains("-T1"))
                return 1;

            if (id.Contains("-T2"))
                return 2;

            if (id.Contains("-T3"))
                return 3;

            if (id.Contains("-T4"))
                return 4;

            if (id.Contains("-T5"))
                return 5;

            throw new System.Exception($"Cannot determine Tier from ID: {id}");
        }
    }
}

