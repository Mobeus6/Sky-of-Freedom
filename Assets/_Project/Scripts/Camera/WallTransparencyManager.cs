using System.Collections.Generic;
using UnityEngine;

public class WallTransparencyManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Camera gameplayCamera;

    [SerializeField]
    private Transform targetPoint;

    [Header("Detection")]
    [SerializeField]
    private LayerMask wallLayer;

    [Tooltip("Ширина області, в якій шукаються стіни.")]
    [SerializeField]
    private float detectionWidth = 4f;

    [Tooltip("Висота області, в якій шукаються стіни.")]
    [SerializeField]
    private float detectionHeight = 4f;

    [Tooltip("Максимальна відстань перевірки.")]
    [SerializeField]
    private float detectionDistance = 50f;

    [Header("Transparency")]
    [SerializeField, Range(0f, 1f)]
    private float transparentAlpha = 0.25f;

    [SerializeField]
    private float fadeSpeed = 5f;

    private readonly HashSet<WallTransparency> currentWalls =
        new HashSet<WallTransparency>();

    private readonly HashSet<WallTransparency> detectedWalls =
        new HashSet<WallTransparency>();

    private readonly List<WallTransparency> wallsToRemove =
        new List<WallTransparency>();

    private RaycastHit[] hits;

    private void Update()
    {
        DetectWalls();
        UpdateWalls();
    }

    private void DetectWalls()
    {
        detectedWalls.Clear();

        if (gameplayCamera == null ||
            targetPoint == null)
        {
            return;
        }

        Vector3 origin =
            gameplayCamera.transform.position;

        Vector3 direction =
            targetPoint.position - origin;

        float distance =
            direction.magnitude;

        if (distance > detectionDistance)
        {
            distance = detectionDistance;
        }

        if (distance <= 0.01f)
        {
            return;
        }

        direction.Normalize();

        Quaternion rotation =
            Quaternion.LookRotation(direction);

        Vector3 halfExtents =
            new Vector3(
                detectionWidth * 0.5f,
                detectionHeight * 0.5f,
                0.1f);

        hits = Physics.BoxCastAll(
            origin,
            halfExtents,
            direction,
            rotation,
            distance,
            wallLayer);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
            {
                continue;
            }

            WallTransparency wall =
                hit.collider.GetComponentInParent<
                    WallTransparency>();

            if (wall == null)
            {
                continue;
            }

            detectedWalls.Add(wall);
        }
    }

    private void UpdateWalls()
    {
        foreach (WallTransparency wall
            in detectedWalls)
        {
            if (wall == null)
            {
                continue;
            }

            if (currentWalls.Contains(wall))
            {
                continue;
            }

            currentWalls.Add(wall);

            wall.SetTransparent(
                transparentAlpha,
                fadeSpeed);
        }

        wallsToRemove.Clear();

        foreach (WallTransparency wall
            in currentWalls)
        {
            if (wall == null)
            {
                wallsToRemove.Add(wall);
                continue;
            }

            if (detectedWalls.Contains(wall))
            {
                continue;
            }

            wall.SetOpaque(fadeSpeed);

            wallsToRemove.Add(wall);
        }

        foreach (WallTransparency wall
            in wallsToRemove)
        {
            currentWalls.Remove(wall);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (gameplayCamera == null ||
            targetPoint == null)
        {
            return;
        }

        Vector3 origin =
            gameplayCamera.transform.position;

        Vector3 direction =
            targetPoint.position - origin;

        float distance =
            direction.magnitude;

        if (distance <= 0.01f)
        {
            return;
        }

        if (distance > detectionDistance)
        {
            distance = detectionDistance;
        }

        direction.Normalize();

        Quaternion rotation =
            Quaternion.LookRotation(direction);

        Vector3 center =
            origin +
            direction *
            distance *
            0.5f;

        Vector3 size =
            new Vector3(
                detectionWidth,
                detectionHeight,
                distance);

        Gizmos.matrix =
            Matrix4x4.TRS(
                center,
                rotation,
                Vector3.one);

        Gizmos.DrawWireCube(
            Vector3.zero,
            size);

        Gizmos.matrix =
            Matrix4x4.identity;
    }
}