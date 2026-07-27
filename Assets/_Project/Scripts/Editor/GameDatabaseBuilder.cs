using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SkyOfFreedom.Data;

namespace SkyOfFreedom.Editor
{
    public static class GameDatabaseBuilder
    {
        public static void Build(GameDatabase database)
        {
            if (database == null)
            {
                Debug.LogError("GameDatabase is null.");
                return;
            }

            SerializedObject serializedDatabase = new SerializedObject(database);

            FillList<MaterialSO>(
                serializedDatabase.FindProperty("materials"));

            FillList<ComponentSO>(
                serializedDatabase.FindProperty("components"));

            FillList<DroneModelSO>(
                serializedDatabase.FindProperty("droneModels"));

            FillList<ResearchSO>(
                serializedDatabase.FindProperty("researches"));
            FillList<LicenseSO>(
    serializedDatabase.FindProperty("licenses"));

            serializedDatabase.ApplyModifiedProperties();

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"Game Database rebuilt.\n" +
                $"Materials: {database.Materials.Count}\n" +
                $"Components: {database.Components.Count}\n" +
                $"Drone Models: {database.DroneModels.Count}\n" +
                $"Researches: {database.Researches.Count}\n" +
                $"Licenses: {database.Licenses.Count}");
            SerializedProperty licenses = serializedDatabase.FindProperty("licenses");

            FillList<LicenseSO>(licenses);

            Debug.Log($"Licenses property size: {licenses.arraySize}");

            serializedDatabase.ApplyModifiedProperties();

            Debug.Log($"Database Licenses Count: {database.Licenses.Count}");
        }

        private static void FillList<T>(SerializedProperty property)
     where T : DataSO
        {
            Debug.Log($"FillList<{typeof(T).Name}>");
            Debug.Log($"Property: {property?.name}");

            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

            Debug.Log($"Found: {guids.Length}");

            List<T> assets = new List<T>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                T asset = AssetDatabase.LoadAssetAtPath<T>(path);

                Debug.Log($"{path} -> {(asset == null ? "NULL" : asset.name)}");

                if (asset != null)
                    assets.Add(asset);
            }

            Debug.Log($"Loaded: {assets.Count}");

            property.ClearArray();

            for (int i = 0; i < assets.Count; i++)
            {
                property.InsertArrayElementAtIndex(i);
                property.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
            }

            Debug.Log($"Serialized size: {property.arraySize}");
        }
        [CustomEditor(typeof(GameDatabase))]
        public class GameDatabaseEditor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                GUILayout.Space(10);

                GUI.backgroundColor = new Color(0.3f, 0.8f, 1f);

                if (GUILayout.Button("Rebuild Database", GUILayout.Height(35)))
                {
                    GameDatabase database = (GameDatabase)target;

                    if (EditorUtility.DisplayDialog(
                            "Rebuild Database",
                            "Automatically find all Materials, Components, Drone Models and Researches in the project and rebuild the Game Database?",
                            "Rebuild",
                            "Cancel"))
                    {
                        GameDatabaseBuilder.Build(database);
                    }
                }

                GUI.backgroundColor = Color.white;

                GUILayout.Space(10);

                GameDatabase gameDatabase = (GameDatabase)target;

                EditorGUILayout.HelpBox(
                    $"Materials: {gameDatabase.Materials.Count}\n" +
                    $"Components: {gameDatabase.Components.Count}\n" +
                    $"Drone Models: {gameDatabase.DroneModels.Count}\n" +
                    $"Researches: {gameDatabase.Researches.Count}\n" +
                $"Licenses: {gameDatabase.Licenses.Count}",
                MessageType.Info);
            }
        }

    }
}
