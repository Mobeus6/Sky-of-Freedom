using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SkyOfFreedom.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class WorkerPatrol : MonoBehaviour
    {
        private static readonly List<WorkerPatrol> workers = new List<WorkerPatrol>();
        public static IReadOnlyList<WorkerPatrol> ActiveWorkers => workers;
        [SerializeField] private Transform routeRoot;
        [SerializeField] private Transform[] routePoints = Array.Empty<Transform>();
        [SerializeField] private Animator animator;
        [SerializeField] private WorkerDoor[] doors = Array.Empty<WorkerDoor>();
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField, Min(0f)] private float waitAtPoint = 3f;
        [SerializeField, Min(.1f)] private float sampleDistance = 1f;
        [SerializeField, Min(.1f)] private float doorLookAhead = 1.5f;
        [SerializeField, Min(1f)] private float stuckTimeout = 12f;
        private NavMeshAgent agent;
        private int pointIndex;
        private bool travelling;
        private bool started;
        private bool hasSpeedParameter;
        private float waitUntil;
        private float nextAttempt;
        private float lastProgressAt;
        private Vector3 lastProgressPosition;
        private NavMeshPath reusablePath;
        public float AgentRadius => agent != null ? agent.radius : .3f;
        public Vector3 BodyPosition => transform.position + Vector3.up * (agent != null ? agent.height * .5f : .9f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { workers.Clear(); }

        public void Configure(Transform route, Transform[] points, Animator modelAnimator, WorkerDoor[] sceneDoors)
        {
            routeRoot = route; routePoints = points; animator = modelAnimator; doors = sceneDoors;
        }

        private void Awake()
        {
            reusablePath = new NavMeshPath();
            agent = GetComponent<NavMeshAgent>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
                foreach (AnimatorControllerParameter parameter in animator.parameters)
                    if (parameter.name == speedParameter && parameter.type == AnimatorControllerParameterType.Float)
                        hasSpeedParameter = true;
                if (!hasSpeedParameter) Debug.LogWarning("Worker Animator needs float parameter: " + speedParameter, this);
            }
            if (routePoints.Length == 0 && routeRoot != null)
            {
                routePoints = new Transform[routeRoot.childCount];
                for (int i = 0; i < routePoints.Length; i++) routePoints[i] = routeRoot.GetChild(i);
            }
        }

        private void OnEnable()
        {
            if (!workers.Contains(this)) workers.Add(this);
            started = false; travelling = false; nextAttempt = 0f;
        }

        private void OnDisable()
        {
            workers.Remove(this);
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            { agent.isStopped = true; agent.ResetPath(); }
            SetAnimationSpeed(0f);
        }

        private void SetAnimationSpeed(float speed)
        {
            if (animator != null && animator.isActiveAndEnabled && hasSpeedParameter)
                animator.SetFloat(speedParameter, speed, .12f, Time.deltaTime);
        }

        private void Update()
        {
            if (agent == null || !agent.enabled || Time.timeScale <= 0f) return;
            if (!started)
            {
                if (Time.time < nextAttempt) return;
                nextAttempt = Time.time + 3f;
                if (!agent.isOnNavMesh)
                {
                    var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
                    if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, sampleDistance, filter) || !agent.Warp(hit.position))
                    { Debug.LogWarning("Worker is not on its NavMesh. Check Bake and spawn position.", this); return; }
                }
                started = true; nextAttempt = 0f; waitUntil = 0f;
            }
            if (!agent.isOnNavMesh) { started = false; SetAnimationSpeed(0f); return; }
            SetAnimationSpeed(agent.velocity.magnitude);
            if (!travelling)
            {
                if (Time.time >= waitUntil && Time.time >= nextAttempt) GoToNextReachablePoint();
                return;
            }

            bool waitForDoor = false;
            Vector3 offset = Vector3.up * agent.height * .5f;
            Vector3 start = BodyPosition;
            float lookAhead = Mathf.Max(doorLookAhead, agent.speed * agent.speed / (2f * Mathf.Max(.1f, agent.acceleration)) + agent.radius + .3f);
            Vector3 end = start + Vector3.ClampMagnitude(agent.steeringTarget + offset - start, lookAhead);
            foreach (WorkerDoor door in doors)
            {
                if (door == null || !door.IntersectsApproach(start, end, agent.radius)) continue;
                door.RequestOpen();
                if (!door.IsPassable) waitForDoor = true;
            }
            agent.isStopped = waitForDoor;
            if (waitForDoor) { lastProgressAt = Time.time; SetAnimationSpeed(0f); return; }
            if (agent.pathPending) return;
            if (agent.pathStatus != NavMeshPathStatus.PathComplete)
            { StopAndRetry(); return; }
            if (agent.remainingDistance <= agent.stoppingDistance + .05f && agent.velocity.sqrMagnitude < .03f)
            {
                agent.isStopped = true; agent.ResetPath(); travelling = false;
                waitUntil = Time.time + waitAtPoint;
                pointIndex = (pointIndex + 1) % Mathf.Max(1, routePoints.Length);
                SetAnimationSpeed(0f);
                return;
            }
            if ((transform.position - lastProgressPosition).sqrMagnitude > .04f)
            { lastProgressPosition = transform.position; lastProgressAt = Time.time; }
            if (Time.time - lastProgressAt > stuckTimeout) StopAndRetry();
        }

        private void GoToNextReachablePoint()
        {
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            for (int i = 0; i < routePoints.Length; i++)
            {
                pointIndex %= routePoints.Length;
                Transform point = routePoints[pointIndex];
                if (point != null && point.gameObject.activeInHierarchy &&
                    NavMesh.SamplePosition(point.position, out NavMeshHit hit, sampleDistance, filter) &&
                    NavMesh.CalculatePath(agent.nextPosition, hit.position, filter, reusablePath) &&
                    reusablePath.status == NavMeshPathStatus.PathComplete && agent.SetPath(reusablePath))
                {
                    travelling = true; agent.isStopped = false;
                    lastProgressAt = Time.time; lastProgressPosition = transform.position;
                    return;
                }
                pointIndex = (pointIndex + 1) % Mathf.Max(1, routePoints.Length);
            }
            agent.isStopped = true;
            nextAttempt = Time.time + 5f;
            SetAnimationSpeed(0f);
            Debug.LogWarning("Worker cannot reach any route point. Check NavMesh and door openings.", this);
        }

        private void StopAndRetry()
        {
            agent.isStopped = true; agent.ResetPath(); travelling = false;
            nextAttempt = Time.time + 2f;
            SetAnimationSpeed(0f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < routePoints.Length; i++)
            {
                if (routePoints[i] == null) continue;
                Gizmos.DrawWireSphere(routePoints[i].position, .15f);
                Transform next = routePoints[(i + 1) % routePoints.Length];
                if (next != null) Gizmos.DrawLine(routePoints[i].position, next.position);
            }
        }
    }
}
