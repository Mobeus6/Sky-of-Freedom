#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
using SkyOfFreedom.Gameplay;

namespace SkyOfFreedom.EditorTools
{
    public static class WorkerSceneTools
    {
        private const string Request = "Temp/SkyWorkerSetup.request";
        private const string Output = "Temp/WorkerSetup";
        [InitializeOnLoadMethod]
        private static void Schedule() { EditorApplication.delayCall += RunRequested; }
        private static void RunRequested()
        {
            if (!File.Exists(Request)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
            { EditorApplication.delayCall += RunRequested; return; }
            string command = File.ReadAllText(Request).Trim();
            File.Delete(Request);
            try { if (command == "configure") Configure(); else Inspect(); }
            catch (Exception e) { Directory.CreateDirectory(Output); File.WriteAllText(Output + "/error.txt", e.ToString()); }
        }

        private static string PathOf(Transform t)
        {
            return t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
        }

        [MenuItem("Tools/Sky of Freedom/Workers/Configure Existing Worker")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            Directory.CreateDirectory(Output);
            Scene scene = SceneManager.GetSceneByPath("Assets/_Project/Scenes/Game.unity");
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Game.unity", OpenSceneMode.Additive);
            Transform factory = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Single(t => PathOf(t) == "Environment/Factory/Factory Lvl 1");
            Transform worker = factory.Find("Worker");
            Transform model = worker.Find("Bearded Man");
            Transform route = factory.Find("Worker Route");
            NavMeshSurface surface = factory.Find("Factory Navigation").GetComponent<NavMeshSurface>();
            if (worker.GetComponent<WorkerPatrol>() != null) throw new InvalidOperationException("Worker is already configured; inspect before changing it again.");
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory("Assets/_Recovery");
            string backup = "Assets/_Recovery/BeforeWorkerSetup-" + stamp + ".unity";
            if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Cannot back up scene.");
            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Configure existing worker and doors");
            Undo.RegisterFullObjectHierarchyUndo(factory.gameObject, "Configure worker");
            var log = new StringBuilder("Backup: " + backup + "\n");
            try
            {
                var doors = new List<WorkerDoor>();
                foreach (Transform wall in factory.Find("Walls"))
                {
                    if (!wall.name.StartsWith("Inner Door Wall", StringComparison.Ordinal)) continue;
                    Transform doorRoot = wall.Find("Door (2)");
                    BoxCollider zone = doorRoot.Find("DetectionZone").GetComponent<BoxCollider>();
                    Transform first = doorRoot.Find("Door_Low");
                    Transform second = doorRoot.Find("Door_Low1");
                    if (first == null || second == null || zone == null) throw new InvalidOperationException("Door structure changed: " + wall.name);
                    WorkerDoor door = Undo.AddComponent<WorkerDoor>(doorRoot.gameObject);
                    door.Configure(zone, new[] {
                        new WorkerDoor.Leaf { Transform = first, OpenAngle = 95 },
                        new WorkerDoor.Leaf { Transform = second, OpenAngle = -95 }
                    });
                    zone.isTrigger = true;
                    foreach (Transform leaf in new[] { first, second })
                    {
                        Rigidbody body = leaf.GetComponent<Rigidbody>();
                        if (body != null) { body.isKinematic = true; body.useGravity = false; }
                        BoxCollider collider = leaf.GetComponent<BoxCollider>();
                        if (collider == null) collider = Undo.AddComponent<BoxCollider>(leaf.gameObject);
                        Bounds meshBounds = leaf.GetComponent<MeshFilter>().sharedMesh.bounds;
                        collider.center = meshBounds.center; collider.size = meshBounds.size;
                        var modifier = leaf.GetComponent<NavMeshModifier>();
                        if (modifier == null) modifier = Undo.AddComponent<NavMeshModifier>(leaf.gameObject);
                        modifier.ignoreFromBuild = true;
                    }
                    SplitDoorway(wall, first, second);
                    doors.Add(door);
                    Vector3 a = first.localPosition, b = second.localPosition;
                    door.Tick(1f, true);
                    if (!door.IsPassable || Vector3.Distance(a, first.localPosition) > .001f || Vector3.Distance(b, second.localPosition) > .001f)
                        throw new InvalidOperationException("Door hinge test failed: " + wall.name);
                    door.Tick(1f, false);
                    log.AppendLine("PASS door opens/closes around original hinges: " + wall.name);
                }
                if (doors.Count != 3) throw new InvalidOperationException("Expected three existing inner doors.");
                NavMeshAgent agent = worker.GetComponent<NavMeshAgent>();
                Vector3 modelPosition = model.position;
                worker.position = new Vector3(modelPosition.x, worker.position.y, modelPosition.z);
                model.position = modelPosition;
                Rigidbody workerBody = worker.GetComponent<Rigidbody>();
                if (workerBody != null) { workerBody.isKinematic = true; workerBody.useGravity = false; }
                CapsuleCollider capsule = worker.GetComponent<CapsuleCollider>();
                if (capsule != null) { capsule.height = agent.height; capsule.radius = agent.radius; capsule.center = Vector3.up * agent.height * .5f; }
                // Include the user's wall layer (3), while leaving other layer choices intact.
                surface.layerMask = surface.layerMask.value | (1 << 3);
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                surface.BuildNavMesh();
                if (surface.navMeshData == null) throw new InvalidOperationException("NavMesh bake failed.");
                var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
                if (!NavMesh.SamplePosition(worker.position, out NavMeshHit spawn, 1f, filter))
                    throw new InvalidOperationException("Worker spawn is not on NavMesh.");
                worker.position = spawn.position;
                Transform[] points = route.Cast<Transform>().ToArray();
                if (points.Length != 5) throw new InvalidOperationException("Expected five route points.");
                Vector3 from = spawn.position;
                for (int i = 0; i <= points.Length; i++)
                {
                    Transform point = points[i % points.Length];
                    var path = new NavMeshPath();
                    if (!NavMesh.SamplePosition(point.position, out NavMeshHit hit, 1f, filter) ||
                        !NavMesh.CalculatePath(from, hit.position, filter, path) || path.status != NavMeshPathStatus.PathComplete)
                        throw new InvalidOperationException("Route cannot reach " + point.name + ". Changes rolled back.");
                    log.AppendLine("PASS reachable route: " + point.name + ", corners=" + path.corners.Length);
                    from = hit.position;
                }
                Animator animator = model.GetComponent<Animator>();
                animator.applyRootMotion = false;
                WorkerPatrol patrol = Undo.AddComponent<WorkerPatrol>(worker.gameObject);
                patrol.Configure(route, points, animator, doors.ToArray());
                AssetDatabase.CreateAsset(surface.navMeshData, "Assets/_Project/Scenes/Game/NavMesh-Worker-" + stamp + ".asset");
                foreach (Component component in factory.GetComponentsInChildren<Component>(true))
                    if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save configured scene.");
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(undo);
                log.AppendLine("SAVED: " + scene.path + "; worker and three doors configured.");
                File.WriteAllText(Output + "/configured.txt", log.ToString());
            }
            catch (Exception e)
            {
                surface.RemoveData();
                Undo.RevertAllDownToGroup(undo);
                surface.AddData();
                log.AppendLine("FAILED: " + e);
                File.WriteAllText(Output + "/configured.txt", log.ToString());
                throw;
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        private static void SplitDoorway(Transform wall, Transform first, Transform second)
        {
            BoxCollider original = wall.GetComponent<BoxCollider>();
            Bounds outer = new Bounds(original.center, original.size);
            Bounds opening = new Bounds(wall.InverseTransformPoint(first.position), Vector3.zero);
            foreach (Transform leaf in new[] { first, second })
            {
                Bounds mesh = leaf.GetComponent<MeshFilter>().sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = mesh.center + Vector3.Scale(mesh.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    opening.Encapsulate(wall.InverseTransformPoint(leaf.TransformPoint(corner)));
                }
            }
            float left = opening.min.z - .04f, right = opening.max.z + .04f, top = opening.max.y + .04f;
            if (left <= outer.min.z || right >= outer.max.z || top >= outer.max.y)
                throw new InvalidOperationException("Door opening exceeds wall bounds: " + wall.name);
            original.enabled = false;
            var container = new GameObject("Worker Doorway Colliders");
            Undo.RegisterCreatedObjectUndo(container, "Doorway colliders");
            container.transform.SetParent(wall, false);
            AddBox(container.transform, "Left", new Vector3(outer.min.x, outer.min.y, outer.min.z), new Vector3(outer.max.x, outer.max.y, left), wall.gameObject.layer);
            AddBox(container.transform, "Right", new Vector3(outer.min.x, outer.min.y, right), outer.max, wall.gameObject.layer);
            AddBox(container.transform, "Lintel", new Vector3(outer.min.x, top, left), new Vector3(outer.max.x, outer.max.y, right), wall.gameObject.layer);
        }

        private static void AddBox(Transform parent, string name, Vector3 min, Vector3 max, int layer)
        {
            var obj = new GameObject(name, typeof(BoxCollider));
            Undo.RegisterCreatedObjectUndo(obj, "Doorway collider");
            obj.layer = layer;
            obj.transform.SetParent(parent, false);
            BoxCollider box = obj.GetComponent<BoxCollider>();
            box.center = (min + max) * .5f; box.size = max - min;
        }

        [MenuItem("Tools/Sky of Freedom/Workers/Inspect Existing Setup")]
        public static void Inspect()
        {
            Directory.CreateDirectory(Output);
            var text = new StringBuilder();
            Scene scene = SceneManager.GetSceneByPath("Assets/_Project/Scenes/Game.unity");
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Game.unity", OpenSceneMode.Additive);
            text.AppendLine("Scene: " + scene.path + "; dirty: " + scene.isDirty);
            Transform[] transforms = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            Transform factory = transforms.Single(t => PathOf(t) == "Environment/Factory/Factory Lvl 1");
            foreach (Transform t in factory.GetComponentsInChildren<Transform>(true))
            {
                string path = PathOf(t);
                bool relevant = path.Contains("Worker") || path.Contains("Door") || path.Contains("Detection") ||
                    path.Contains("Factory Navigation") || t.GetComponent<Collider>() != null;
                if (!relevant) continue;
                text.AppendLine(path + " active=" + t.gameObject.activeInHierarchy + " layer=" + t.gameObject.layer +
                    " world=" + t.position.ToString("F3") + " local=" + t.localPosition.ToString("F3") +
                    " euler=" + t.localEulerAngles.ToString("F2") + " scale=" + t.lossyScale.ToString("F3"));
                text.AppendLine("  components: " + string.Join(", ", t.GetComponents<Component>().Select(c => c == null ? "MISSING" : c.GetType().Name)));
                foreach (Renderer r in t.GetComponents<Renderer>()) text.AppendLine("  rendererBounds " + r.bounds);
                foreach (Collider c in t.GetComponents<Collider>()) text.AppendLine("  collider " + c.GetType().Name + " enabled=" + c.enabled + " trigger=" + c.isTrigger + " bounds=" + c.bounds);
                MeshFilter mesh = t.GetComponent<MeshFilter>();
                if (mesh != null && mesh.sharedMesh != null && path.Contains("Door")) text.AppendLine("  meshBounds " + mesh.sharedMesh.bounds);
                foreach (Animator animator in t.GetComponents<Animator>())
                {
                    text.AppendLine("  animator: " + AssetDatabase.GetAssetPath(animator.runtimeAnimatorController) + " rootMotion=" + animator.applyRootMotion);
                    if (animator.runtimeAnimatorController != null)
                        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                            text.AppendLine("  clip " + clip.name + " legacy=" + clip.legacy + " loop=" + clip.isLooping + " duration=" + clip.length);
                }
                foreach (Animation a in t.GetComponents<Animation>())
                    foreach (AnimationState state in a) text.AppendLine("  legacy clip " + state.name);
                foreach (NavMeshSurface surface in t.GetComponents<NavMeshSurface>())
                    text.AppendLine("  surface data=" + AssetDatabase.GetAssetPath(surface.navMeshData));
            }
            Transform route = factory.Find("Worker Route");
            Transform worker = factory.Find("Worker");
            text.AppendLine("PATH CHECKS:");
            Vector3 from = worker.position;
            foreach (Transform point in route)
            {
                bool sampled = NavMesh.SamplePosition(point.position, out NavMeshHit hit, 3f, NavMesh.AllAreas);
                var path = new NavMeshPath();
                bool calculated = sampled && NavMesh.CalculatePath(from, hit.position, NavMesh.AllAreas, path);
                text.AppendLine(point.name + " sampled=" + sampled + " nearest=" + hit.position + " path=" + calculated + " status=" + path.status);
                if (sampled) from = hit.position;
            }
            File.WriteAllText(Output + "/inspection.txt", text.ToString());
            Debug.Log("Worker setup inspection written: " + Output);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
#endif
