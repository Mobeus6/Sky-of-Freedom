using SkyOfFreedom.Data;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace SkyOfFreedom.Editor
{
    public static class LicenseImporter
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

                LicenseSO license =
                    AssetUtility.LoadByID<LicenseSO>(outputFolder, id);

                if (license == null)
                {
                    license = AssetUtility.CreateAsset<LicenseSO>(
                        outputFolder,
                        id);

                    created++;
                }
                else
                {
                    updated++;
                }

                ComponentSO component =
                    AssetUtility.LoadByID<ComponentSO>(
                        "Assets/_Project/Data/ScriptableObjects/Components",
                        row["Component ID"]);

                if (component == null)
                {
                    Debug.LogWarning(
                        $"Component not found: {row["Component ID"]}");
                }

                license.SetData(
                    id,
                    row["License"],
                    row["Description"],
                    component,
                    null,
                    row.GetInt("Required Factory Lv"),
                    row.GetInt("Purchase Cost"));

                EditorUtility.SetDirty(license);
            }

            AssetUtility.Save();

            Debug.Log(
                $"License import completed.\nCreated: {created}\nUpdated: {updated}");
        }
    }
}