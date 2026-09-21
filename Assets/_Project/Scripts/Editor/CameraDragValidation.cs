#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SkyOfFreedom.Gameplay.Factory;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SkyOfFreedom.EditorTools
{
    public static class CameraDragValidation
    {
        private const string Request = "Temp/CameraDragValidation.request";
        private static readonly List<string> Results = new List<string>();
        private static void Check(bool ok, string label) { Results.Add((ok ? "PASS " : "FAIL ") + label); }
        [InitializeOnLoadMethod]
        private static void Schedule() { EditorApplication.delayCall += Requested; }
        private static void Requested()
        {
            if (!File.Exists(Request)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorApplication.delayCall += Requested; return; }
            File.Delete(Request);
            Run();
        }
        [MenuItem("Tools/Sky of Freedom/Validate Camera Drag")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Results.Clear();
            var root = new GameObject("Camera Drag Validation (temporary)");
            root.SetActive(false);
            root.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var ui = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
                ui.transform.SetParent(root.transform);
                var physics = root.AddComponent<PhysicsRaycaster>();
                var classify = typeof(GameplayCameraController).GetMethod("IsUIHit", BindingFlags.NonPublic | BindingFlags.Static);
                Check(!(bool)classify.Invoke(null, new object[] { new RaycastResult { module=physics } }), "World collider does not block camera");
                Check((bool)classify.Invoke(null, new object[] { new RaycastResult { module=ui.GetComponent<GraphicRaycaster>() } }), "Canvas UI blocks camera");
                Check(!(bool)classify.Invoke(null, new object[] { new RaycastResult() }), "Empty hit does not block camera");
                Check(!GameplayCameraController.ExceedsDragThreshold(Vector2.zero,Vector2.zero), "Stationary press is a tap");
                Check(GameplayCameraController.ExceedsDragThreshold(Vector2.zero,new Vector2(10000,0)), "Swipe exceeds threshold");
                var zone = root.AddComponent<FactoryZoneInteraction>();
                var pointer = new PointerEventData(EventSystem.current) { eligibleForClick=true, pressPosition=new Vector2(30,30), position=new Vector2(30,30) };
                zone.OnPointerClick(pointer);
                Check(zone.IsSelected, "Short tap selects zone");
                zone.OnBeginDrag(pointer);
                Check(!pointer.eligibleForClick && !zone.IsSelected, "Drag cancels selection and click");
                zone.OnEndDrag(pointer);
                zone.OnPointerClick(pointer);
                Check(!zone.IsSelected, "Drag returning to start does not select on release");
                pointer.eligibleForClick=true;
                pointer.position = new Vector2(10000,30);
                zone.OnPointerClick(pointer);
                Check(!zone.IsSelected, "Release with displacement is not a tap");
                pointer.position=pointer.pressPosition;
                pointer.button=PointerEventData.InputButton.Right;
                zone.OnPointerClick(pointer);
                Check(!zone.IsSelected, "Right click does not select zone");
                var camera = root.AddComponent<GameplayCameraController>();
                typeof(GameplayCameraController).GetField("isMovingToZone",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(camera,true);
                int movements=0;
                camera.UserStartedCameraMovement += () => movements++;
                typeof(GameplayCameraController).GetMethod("NotifyUserStartedCameraMovement",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(camera,null);
                Check(!camera.IsMovingToZone && movements==1, "Manual movement interrupts auto camera movement");
            }
            catch (Exception e) { Results.Add("FAIL " + e); }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            Directory.CreateDirectory("Temp/CameraDragValidation");
            File.WriteAllLines("Temp/CameraDragValidation/results.txt", Results);
        }
    }
}
#endif
