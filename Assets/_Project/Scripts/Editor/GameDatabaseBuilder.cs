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

            serializedDatabase.ApplyModifiedProperties();

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"Game Database rebuilt.\n" +
                $"Materials: {database.Materials.Count}\n" +
                $"Components: {database.Components.Count}\n" +
                $"Drone Models: {database.DroneModels.Count}\n" +
                $"Researches: {database.Researches.Count}");
        }

        private static void FillList<T>(SerializedProperty property)
            where T : DataSO
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

            List<T> assets = new List<T>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                T asset = AssetDatabase.LoadAssetAtPath<T>(path);

                if (asset != null)
                {
                    assets.Add(asset);
                }
            }

            assets = assets
                .OrderBy(a => a.ID)
                .ToList();

            property.ClearArray();

            for (int i = 0; i < assets.Count; i++)
            {
                property.InsertArrayElementAtIndex(i);

                property
                    .GetArrayElementAtIndex(i)
                    .objectReferenceValue = assets[i];
            }
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
                    $"Researches: {gameDatabase.Researches.Count}",
                    MessageType.Info);
            }
        }
    }
}
