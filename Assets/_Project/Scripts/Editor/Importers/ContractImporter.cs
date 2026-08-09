using System.Collections.Generic;
using SkyOfFreedom.Contracts;
using SkyOfFreedom.Data;
using UnityEditor;
using UnityEngine;

namespace SkyOfFreedom.Editor
{
    public static class ContractImporter
    {
        private const string ContractFolder =
            "Assets/_Project/Data/ScriptableObjects/Contracts";

        private const string ComponentFolder =
            "Assets/_Project/Data/ScriptableObjects/Components";

        private const string DroneFolder =
            "Assets/_Project/Data/ScriptableObjects/Drones";

        private const string LicenseFolder =
            "Assets/_Project/Data/ScriptableObjects/Licenses";

        private const string DatabaseSearchFilter =
            "t:GameDatabase";

        public static void Import(string csvPath, string outputFolder)
        {
            List<CsvRow> rows = CsvReader.Read(csvPath);

            int created = 0;
            int updated = 0;
            int skipped = 0;
            int errors = 0;

            foreach (CsvRow row in rows)
            {
                string id = row["ID"];
                string category = row["Category"];

                if (string.IsNullOrWhiteSpace(id))
                {
                    skipped++;
                    continue;
                }

                if (category == "Component")
                {
                    Debug.LogWarning(
                        $"ContractImporter: Skipping legacy duplicate row " +
                        $"with ID '{id}' and Category 'Component'. " +
                        $"Use the 'Components' row instead.");

                    skipped++;
                    continue;
                }

                if (category != "Components" &&
                    category != "Drone")
                {
                    Debug.LogWarning(
                        $"ContractImporter: Unknown category '{category}' " +
                        $"for contract '{id}'. Row skipped.");

                    skipped++;
                    continue;
                }

                ContractSO contract =
                    AssetUtility.LoadByID<ContractSO>(
                        outputFolder,
                        id);

                if (contract == null)
                {
                    contract =
                        AssetUtility.CreateAsset<ContractSO>(
                            outputFolder,
                            id);

                    created++;
                }
                else
                {
                    updated++;
                }

                ContractTargetType targetType;
                ComponentSO component = null;
                DroneModelSO droneModel = null;

                string targetID = row["ID Whats needed"];

                if (category == "Components")
                {
                    targetType = ContractTargetType.Component;

                    component =
                        AssetUtility.LoadByID<ComponentSO>(
                            ComponentFolder,
                            targetID);

                    if (component == null)
                    {
                        Debug.LogError(
                            $"ContractImporter: Component not found: " +
                            $"{targetID} for contract {id}");

                        errors++;
                        continue;
                    }
                }
                else
                {
                    targetType = ContractTargetType.Drone;

                    droneModel =
                        AssetUtility.LoadByID<DroneModelSO>(
                            DroneFolder,
                            targetID);

                    if (droneModel == null)
                    {
                        Debug.LogError(
                            $"ContractImporter: Drone model not found: " +
                            $"{targetID} for contract {id}");

                        errors++;
                        continue;
                    }
                }

                LicenseSO license = null;

                string licenseID = row["License needed"];

                if (!string.IsNullOrWhiteSpace(licenseID))
                {
                    license =
                        AssetUtility.LoadByID<LicenseSO>(
                            LicenseFolder,
                            licenseID);

                    if (license == null)
                    {
                        Debug.LogWarning(
                            $"ContractImporter: License not found: " +
                            $"{licenseID} for contract {id}");
                    }
                }

                contract.SetData(
                    id,
                    row["Name"],
                    row["Description"],
                    targetType,
                    component,
                    droneModel,
                    null,
                    license,
                    row.GetInt("Reputation needed"),
                    row.GetInt("Reputation Reward"),
                    row.GetInt("Reputation Penalty"));

                int quantity = row.GetInt("Quantity");

                if (quantity < 1)
                {
                    quantity = 1;
                }

                SetQuantity(contract, quantity);

                EditorUtility.SetDirty(contract);
            }

            AssetUtility.Save();

            RebuildGameDatabase();

            Debug.Log(
                "Contract import completed.\n" +
                $"Created: {created}\n" +
                $"Updated: {updated}\n" +
                $"Skipped: {skipped}\n" +
                $"Errors: {errors}");
        }

        private static void SetQuantity(
            ContractSO contract,
            int quantity)
        {
            SerializedObject serializedContract =
                new SerializedObject(contract);

            SerializedProperty minQuantity =
                serializedContract.FindProperty(
                    "minQuantity");

            SerializedProperty maxQuantity =
                serializedContract.FindProperty(
                    "maxQuantity");

            if (minQuantity != null)
            {
                minQuantity.intValue = quantity;
            }

            if (maxQuantity != null)
            {
                maxQuantity.intValue = quantity;
            }

            serializedContract.ApplyModifiedProperties();

            EditorUtility.SetDirty(contract);
        }

        private static void RebuildGameDatabase()
        {
            string[] guids =
                AssetDatabase.FindAssets(
                    DatabaseSearchFilter);

            if (guids.Length == 0)
            {
                Debug.LogError(
                    "ContractImporter: GameDatabase asset was not found.");

                return;
            }

            if (guids.Length > 1)
            {
                Debug.LogWarning(
                    $"ContractImporter: Found {guids.Length} " +
                    "GameDatabase assets. Using the first one.");
            }

            string path =
                AssetDatabase.GUIDToAssetPath(
                    guids[0]);

            GameDatabase database =
                AssetDatabase.LoadAssetAtPath<GameDatabase>(
                    path);

            if (database == null)
            {
                Debug.LogError(
                    $"ContractImporter: Failed to load GameDatabase at {path}");

                return;
            }

            SerializedObject serializedDatabase =
                new SerializedObject(database);

            SerializedProperty contractsProperty =
                serializedDatabase.FindProperty("contracts");

            if (contractsProperty == null)
            {
                Debug.LogError(
                    "ContractImporter: GameDatabase does not contain " +
                    "a 'contracts' list.");

                return;
            }

            string[] contractGuids =
                AssetDatabase.FindAssets(
                    "t:ContractSO",
                    new[] { ContractFolder });

            List<ContractSO> contracts =
                new List<ContractSO>();

            foreach (string contractGuid in contractGuids)
            {
                string contractPath =
                    AssetDatabase.GUIDToAssetPath(
                        contractGuid);

                ContractSO contract =
                    AssetDatabase.LoadAssetAtPath<ContractSO>(
                        contractPath);

                if (contract != null)
                {
                    contracts.Add(contract);
                }
            }

            contracts.Sort(
                (a, b) =>
                    string.Compare(
                        a.ID,
                        b.ID,
                        System.StringComparison.Ordinal));

            contractsProperty.ClearArray();

            for (int i = 0; i < contracts.Count; i++)
            {
                contractsProperty.InsertArrayElementAtIndex(i);

                contractsProperty
                    .GetArrayElementAtIndex(i)
                    .objectReferenceValue = contracts[i];
            }

            serializedDatabase.ApplyModifiedProperties();

            EditorUtility.SetDirty(database);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"ContractImporter: GameDatabase rebuilt. " +
                $"Contracts: {contracts.Count}");
        }
    }
}