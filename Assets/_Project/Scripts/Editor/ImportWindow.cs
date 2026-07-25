using UnityEditor;
using UnityEngine;

namespace SkyOfFreedom.Editor
{
    public class ImportWindow : EditorWindow
    {
        private const string DroneFolder = "Assets/Data/Drones";
        private const string ResearchFolder = "Assets/Data/Research";

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
        }
    }
}