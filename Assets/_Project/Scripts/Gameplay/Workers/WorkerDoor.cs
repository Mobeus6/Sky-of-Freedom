using System;
using UnityEngine;

namespace SkyOfFreedom.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class WorkerDoor : MonoBehaviour
    {
        [Serializable]
        public class Leaf
        {
            public Transform Transform;
            [Tooltip("Hinge position in this leaf's local mesh coordinates.")]
            public Vector3 HingeLocalPoint;
            public float OpenAngle = 95f;
            [NonSerialized] public Vector3 ClosedPosition;
            [NonSerialized] public Quaternion ClosedRotation;
        }

        [SerializeField] private BoxCollider detectionZone;
        [SerializeField] private Leaf[] leaves = Array.Empty<Leaf>();
        [SerializeField, Min(.1f)] private float openingSeconds = .7f;
        [SerializeField, Min(0f)] private float closeDelay = 1.8f;
        private float openAmount;
        private float holdUntil;
        private bool initialized;
        public bool IsPassable => isActiveAndEnabled && openAmount >= .999f;
        public float OpenAmount => openAmount;
        public BoxCollider DetectionZone => detectionZone;

        public void Configure(BoxCollider zone, Leaf[] doorLeaves)
        {
            detectionZone = zone;
            leaves = doorLeaves;
        }

        private void Awake() { Initialize(); }

        private void Initialize()
        {
            if (initialized) return;
            foreach (Leaf leaf in leaves)
            {
                if (leaf?.Transform == null) continue;
                leaf.ClosedPosition = leaf.Transform.localPosition;
                leaf.ClosedRotation = leaf.Transform.localRotation;
            }
            initialized = true;
        }

        public void RequestOpen()
        {
            if (!isActiveAndEnabled) return;
            holdUntil = Time.time + closeDelay + openingSeconds;
        }

        // Geometric look-ahead avoids reliance on Rigidbody/trigger callback ordering.
        // Call with the worker's body centre and its next path corner, not final destination.
        public bool IntersectsApproach(Vector3 start, Vector3 end, float radius)
        {
            if (!isActiveAndEnabled || detectionZone == null || !detectionZone.enabled) return false;
            Transform zone = detectionZone.transform;
            Vector3 scale = zone.lossyScale;
            Vector3 padding = new Vector3(radius / Mathf.Max(.001f, Mathf.Abs(scale.x)),
                radius / Mathf.Max(.001f, Mathf.Abs(scale.y)), radius / Mathf.Max(.001f, Mathf.Abs(scale.z)));
            Bounds bounds = new Bounds(detectionZone.center, detectionZone.size + padding * 2f);
            Vector3 a = zone.InverseTransformPoint(start);
            Vector3 b = zone.InverseTransformPoint(end);
            if (bounds.Contains(a)) return true;
            Vector3 delta = b - a;
            return delta.sqrMagnitude > .0001f &&
                bounds.IntersectRay(new Ray(a, delta.normalized), out float distance) && distance <= delta.magnitude;
        }

        private void Update()
        {
            foreach (WorkerPatrol worker in WorkerPatrol.ActiveWorkers)
            {
                if (worker == null || !worker.isActiveAndEnabled) continue;
                if (IntersectsApproach(worker.BodyPosition, worker.BodyPosition, worker.AgentRadius))
                    holdUntil = Time.time + closeDelay;
            }
            Tick(Time.deltaTime, Time.time < holdUntil);
        }

        public void Tick(float deltaTime, bool shouldOpen)
        {
            Initialize();
            openAmount = Mathf.MoveTowards(openAmount, shouldOpen ? 1f : 0f,
                Mathf.Max(0f, deltaTime) / Mathf.Max(.1f, openingSeconds));
            float eased = Mathf.SmoothStep(0f, 1f, openAmount);
            foreach (Leaf leaf in leaves)
            {
                if (leaf?.Transform == null) continue;
                Quaternion rotation = leaf.ClosedRotation * Quaternion.AngleAxis(leaf.OpenAngle * eased, Vector3.up);
                Vector3 pivot = Vector3.Scale(leaf.HingeLocalPoint, leaf.Transform.localScale);
                leaf.Transform.localRotation = rotation;
                leaf.Transform.localPosition = leaf.ClosedPosition + leaf.ClosedRotation * pivot - rotation * pivot;
            }
        }

        private void OnDisable()
        {
            if (!initialized) return;
            foreach (Leaf leaf in leaves)
            {
                if (leaf?.Transform == null) continue;
                leaf.Transform.localPosition = leaf.ClosedPosition;
                leaf.Transform.localRotation = leaf.ClosedRotation;
            }
            openAmount = 0f;
            holdUntil = 0f;
        }
    }
}
