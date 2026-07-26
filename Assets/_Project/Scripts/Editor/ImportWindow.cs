using UnityEditor;
using UnityEngine;

namespace SkyOfFreedom.Editor
{
    public class ImportWindow : EditorWindow
    {
        private const string DroneFolder = "Assets/_Project/Data/ScriptableObjects/Drones";
        private const string ResearchFolder = "Assets/_Project/Data/ScriptableObjects/Research";
        private const string LicenseFolder = "Assets/_Project/Data/ScriptableObjects/Licenses";
        private const string ComponentFolder = "Assets/_Project/Data/ScriptableObjects/Components";

        [MenuItem("Tools/Sky of Freedom/Import")]
        public static void ShowWindow()
        {
            GetWindow<ImportWindow>("CSV Import");
        }

        private void OnGUI()
        {
            GUILayout.Space(10);

            GUILayout.Label("Sky of Freedom CSV Import", EditorStyles.boldLabel);

            GUILayout.Space(15);

            if (GUILayout.Button("Import Drone Models", GUILayout.Height(40)))
            {
                string file = EditorUtility.OpenFilePanel(
                    "Select Drone Models CSV",
                    "",
                    "csv");

                if (!string.IsNullOrEmpty(file))
                {
                    DroneImporter.Import(file, DroneFolder);
                }
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Import Research", GUILayout.Height(40)))
            {
                string file = EditorUtility.OpenFilePanel(
                    "Select Research CSV",
                    "",
                    "csv");

                if (!string.IsNullOrEmpty(file))
                {
                    ResearchImporter.Import(file, ResearchFolder);
                }
            }
            GUILayout.Space(10);

            if (GUILayout.Button("Import Licenses", GUILayout.Height(40)))
            {
                string file = EditorUtility.OpenFilePanel(
                    "Select Licenses CSV",
                    "",
                    "csv");

                if (!string.IsNullOrEmpty(file))
                {
                    LicenseImporter.Import(file, LicenseFolder);
                }
            }
            GUILayout.Space(10);

            if (GUILayout.Button("Import Components", GUILayout.Height(40)))
            {
                string file = EditorUtility.OpenFilePanel(
                    "Select Components CSV",
                    "",
                    "csv");

                if (!string.IsNullOrEmpty(file))
                {
                    ComponentImporter.Import(file, ComponentFolder);
                }
            }
        }

    }
}