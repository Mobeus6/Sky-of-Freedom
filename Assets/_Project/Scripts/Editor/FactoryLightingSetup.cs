#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SkyOfFreedom.EditorTools
{
    /// <summary>One-time authoring operation. No runtime settings override.</summary>
    public static class FactoryLightingSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string Request = "Temp/FactoryLightingSetup.request";
        private const string MaterialPath = "Assets/_Project/Art/Materials/FactoryStableShadow.mat";
        private const string ShadowName = "Stable Shadow Caster";
        private const uint Exterior = 1;
        private const uint Interior = 2;
        [InitializeOnLoadMethod]
        private static void Schedule() { EditorApplication.delayCall += Requested; }
        private static void Requested()
        {
            if (!File.Exists(Request)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorApplication.delayCall += Requested; return; }
            File.Delete(Request);
            Apply();
        }
        [MenuItem("Tools/Sky of Freedom/Apply Factory Lighting Separation")]
        public static void Apply()
        {
            var report = new List<string>();
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = false;
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
                if (scene.IsValid() && scene.isDirty) throw new InvalidOperationException("Game scene has unsaved changes. Save it, then run this menu command again.");
                if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive); opened = true; }
                Transform environment = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Environment")?.transform;
                Transform level = environment != null ? environment.Find("Factory/Factory Lvl 1") : null;
                if (level == null) throw new InvalidOperationException("Expected Factory Lvl 1 not found; no edits made.");
                Transform lighting = level.Find("Night Lighting");
                if (lighting == null) throw new InvalidOperationException("Night Lighting not found; no edits made.");
                Light[] indoor = Enumerable.Range(2,6).Select(i => lighting.Find("Spot Light (" + i + ")")?.GetComponent<Light>()).ToArray();
                if (indoor.Any(l => l == null || l.type != LightType.Spot)) throw new InvalidOperationException("Expected six interior spotlights not found; no edits made.");
                foreach (string name in new[] { "Ground", "Factory Zones", "Walls", "Ceiling", "Worker" })
                    if (level.Find(name) == null) throw new InvalidOperationException("Missing " + name + "; no edits made.");
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP Lit shader unavailable.");
                var mobile = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/_Project/Settings/Mobile_RPAsset.asset");
                var pc = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/_Project/Settings/PC_RPAsset.asset");
                if (mobile == null || pc == null || EditorUtility.IsDirty(mobile) || EditorUtility.IsDirty(pc))
                    throw new InvalidOperationException("Save pipeline settings first; expected clean Mobile and PC assets.");
                string backup = "Assets/_Recovery/Lighting-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                Directory.CreateDirectory(backup);
                File.Copy(ScenePath, backup + "/Game.unity");
                foreach (var asset in new[] { mobile, pc }) File.Copy(AssetDatabase.GetAssetPath(asset), backup + "/" + asset.name + ".asset");
                report.Add("BACKUP " + backup);
                Material shadow = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                if (shadow == null)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
                    shadow = new Material(shader) { name = "FactoryStableShadow" };
                    shadow.SetFloat("_Cull", 0f);
                    shadow.SetFloat("_AlphaClip", 0f);
                    AssetDatabase.CreateAsset(shadow, MaterialPath);
                }
                int interiors = 0;
                foreach (string name in new[] { "Ground", "Factory Zones", "Worker" })
                    foreach (Renderer renderer in level.Find(name).GetComponentsInChildren<Renderer>(true))
                    { SetLayer(renderer, Interior); interiors++; }
                foreach (Transform wall in level.Find("Walls"))
                    foreach (Renderer renderer in wall.GetComponentsInChildren<Renderer>(true))
                        SetLayer(renderer, wall.name.StartsWith("Inner", StringComparison.Ordinal) ? Interior : Exterior);
                Transform floor = environment.Find("Outer Decoration/Floor");
                if (floor != null)
                    foreach (Renderer renderer in floor.GetComponentsInChildren<Renderer>(true)) SetLayer(renderer, Exterior);
                Light[] lights = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Light>(true)).ToArray();
                foreach (Light light in lights)
                {
                    bool inside = indoor.Contains(light);
                    Undo.RecordObject(light, "Separate factory lighting");
                    light.renderingLayerMask = (int)(inside ? Interior : Exterior);
                    var data = light.GetComponent<UniversalAdditionalLightData>();
                    if (data == null) data = Undo.AddComponent<UniversalAdditionalLightData>(light.gameObject);
                    Undo.RecordObject(data, "Separate factory lighting");
                    data.renderingLayers = inside ? Interior : Exterior;
                    data.customShadowLayers = false;
                    if (inside)
                    {
                        light.shadows = LightShadows.Hard;
                        light.shadowResolution = LightShadowResolution.Medium;
                        light.shadowBias = .05f;
                        light.shadowNormalBias = .2f;
                        light.lightmapBakeType = LightmapBakeType.Realtime;
                        // These are room lights, not night-only exterior lamps.
                        light.enabled = true;
                    }
                    PrefabUtility.RecordPrefabInstancePropertyModifications(light);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(data);
                    report.Add("LIGHT " + light.name + " mask=" + light.renderingLayerMask + " shadows=" + light.shadows);
                }
                // Do not let day/night switch off the only sources lighting the interior.
                foreach (var clock in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SkyOfFreedom.Gameplay.FactoryDayNight>(true)))
                {
                    var serialized = new SerializedObject(clock);
                    var array = serialized.FindProperty("nightLights");
                    for (int i = array.arraySize - 1; i >= 0; i--)
                        if (indoor.Contains(array.GetArrayElementAtIndex(i).objectReferenceValue as Light))
                        {
                            array.GetArrayElementAtIndex(i).objectReferenceValue = null;
                            array.DeleteArrayElementAtIndex(i);
                        }
                    serialized.ApplyModifiedProperties();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(clock);
                }
                int casters = 0;
                foreach (string part in new[] { "Walls", "Ceiling" })
                    foreach (MeshRenderer renderer in level.Find(part).GetComponentsInChildren<MeshRenderer>(true))
                    {
                        if (renderer.name == ShadowName) continue;
                        var filter = renderer.GetComponent<MeshFilter>();
                        if (filter == null || filter.sharedMesh == null) continue;
                        if (renderer.GetComponentInParent<WallTransparency>() == null) continue;
                        Transform existing = renderer.transform.Find(ShadowName);
                        if (existing == null)
                        {
                            var proxy = new GameObject(ShadowName, typeof(MeshFilter), typeof(MeshRenderer));
                            Undo.RegisterCreatedObjectUndo(proxy, "Stable wall shadows");
                            proxy.transform.SetParent(renderer.transform, false);
                            proxy.layer = renderer.gameObject.layer;
                            proxy.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                            var caster = proxy.GetComponent<MeshRenderer>();
                            caster.sharedMaterials = Enumerable.Repeat(shadow, filter.sharedMesh.subMeshCount).ToArray();
                            caster.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                            caster.receiveShadows = false;
                            caster.renderingLayerMask = Exterior | Interior;
                            caster.lightProbeUsage = LightProbeUsage.Off;
                            caster.reflectionProbeUsage = ReflectionProbeUsage.Off;
                        }
                        else
                        {
                            var caster = existing.GetComponent<MeshRenderer>();
                            if (caster != null) SetLayer(caster, Exterior | Interior);
                        }
                        Undo.RecordObject(renderer, "Stable wall shadows");
                        renderer.shadowCastingMode = ShadowCastingMode.Off;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                        casters++;
                    }
                foreach (var pipeline in new[] { mobile, pc })
                {
                    var settings = new SerializedObject(pipeline);
                    settings.FindProperty("m_AdditionalLightShadowsSupported").boolValue = true;
                    // Keep the existing rendering path; accommodate the eight authored spotlights.
                    settings.FindProperty("m_AdditionalLightsPerObjectLimit").intValue = 8;
                    settings.FindProperty("m_SupportsLightLayers").boolValue = true;
                    settings.ApplyModifiedProperties();
                    AssetDatabase.SaveAssetIfDirty(pipeline);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed.");
                report.Add("APPLIED interior renderers=" + interiors + "; stable casters=" + casters);
                report.Add("VERIFY PHONE: camera movement, day/night, interior shadows, frame rate. Ambient lighting remains global.");
            }
            catch (Exception e) { report.Add("NOT COMPLETED " + e); }
            finally
            {
                if (opened && scene.IsValid() && !scene.isDirty) EditorSceneManager.CloseScene(scene, true);
                Directory.CreateDirectory("Temp/FactoryLightingSetup");
                File.WriteAllLines("Temp/FactoryLightingSetup/results.txt", report);
            }
        }
        private static void SetLayer(Renderer renderer, uint mask)
        {
            Undo.RecordObject(renderer, "Separate factory lighting");
            renderer.renderingLayerMask = mask;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
    }
}
#endif
