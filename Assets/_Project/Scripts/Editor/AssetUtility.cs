using System.IO;
using UnityEditor;
using UnityEngine;

namespace SkyOfFreedom.Editor
{
    using SkyOfFreedom.Data;
    public static class AssetUtility
    {
        public static T LoadByID<T>(string folderPath, string id) where T : DataSO
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folderPath });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                T asset = AssetDatabase.LoadAssetAtPath<T>(path);

                if (asset != null && asset.ID == id)
                {
                    return asset;
                }
            }

            return null;
        }

        public static T CreateAsset<T>(string folderPath, string fileName)
            where T : ScriptableObject
        {
            EnsureFolder(folderPath);

            T asset = ScriptableObject.CreateInstance<T>();

            string path = Path.Combine(folderPath, fileName + ".asset");
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            AssetDatabase.CreateAsset(asset, path);

            return asset;
        }

        public static void Save()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] folders = folderPath.Split('/');

            string current = folders[0];

            for (int i = 1; i < folders.Length; i++)
            {
                string next = current + "/" + folders[i];

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, folders[i]);
                }

                current = next;
            }
        }
    }
}