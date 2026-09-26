using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SkyOfFreedom.Gameplay.Factory
{
    public class FactoryZoneInteraction :
        MonoBehaviour,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [Header("Zone Highlight")]
        [SerializeField]
        private GameObject zoneHighlight;

        [SerializeField]
        private GameObject zoneBorder;

        [Header("Zone Camera")]
        [SerializeField]
        private Transform zoneArea;

        [SerializeField]
        private GameplayCameraController cameraController;

        [Header("Highlight State")]
        [SerializeField]
        private bool highlightOnStart = false;

        private bool isSelected;
        private Coroutine upgradePulse;
        private bool pulsing;
        public bool IsUpgradeCameraMoving => cameraController != null && cameraController.isActiveAndEnabled && cameraController.IsUpgradeFocusMoving;

        public void FocusUpgradeCamera()
        {
            if (cameraController != null && zoneArea != null)
                cameraController.FocusZoneUpgrade(zoneArea);
        }
        private readonly List<PulseSurface> pulseSurfaces = new List<PulseSurface>();

        private sealed class PulseSurface
        {
            public Renderer Renderer;
            public int Index;
            public int Property;
            public Color Color;
            public MaterialPropertyBlock Original;
            public MaterialPropertyBlock Animated;
        }

        public void PulseUpgrade(float duration)
        {
            StopUpgradePulse();
            if (!isActiveAndEnabled) return;
            pulsing = true;
            CapturePulseSurfaces(zoneHighlight);
            if (zoneBorder != zoneHighlight) CapturePulseSurfaces(zoneBorder);
            UpdateHighlight();
            upgradePulse = StartCoroutine(AnimatePulse(Mathf.Max(.1f, duration)));
        }

        private void CapturePulseSurfaces(GameObject root)
        {
            if (root == null) return;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null) continue;
                    if (pulseSurfaces.Exists(s => s.Renderer == renderer && s.Index == i)) continue;
                    int property = Shader.PropertyToID(materials[i].HasProperty("_BaseColor") ? "_BaseColor" : "_Color");
                    if (!materials[i].HasProperty(property)) continue;
                    var original = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(original, i);
                    var animated = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(animated, i);
                    // Preserve renderer-wide overrides when there is no per-material block.
                    if (animated.isEmpty) renderer.GetPropertyBlock(animated);
                    Color color = animated.HasColor(property) ? animated.GetColor(property) : materials[i].GetColor(property);
                    pulseSurfaces.Add(new PulseSurface { Renderer = renderer, Index = i, Property = property,
                        Color = color, Original = original, Animated = animated });
                }
            }
        }

        private IEnumerator AnimatePulse(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float wave = .5f - .5f * Mathf.Cos(elapsed / duration * Mathf.PI * 4f);
                foreach (var surface in pulseSurfaces)
                {
                    if (surface.Renderer == null) continue;
                    Color color = surface.Color;
                    float brightness = Mathf.Lerp(.45f, 1.6f, wave);
                    color.r *= brightness; color.g *= brightness; color.b *= brightness;
                    color.a *= Mathf.Lerp(.35f, 1f, wave);
                    surface.Animated.SetColor(surface.Property, color);
                    surface.Renderer.SetPropertyBlock(surface.Animated, surface.Index);
                }
                elapsed += Mathf.Min(Time.unscaledDeltaTime, .05f);
                yield return null;
            }
            upgradePulse = null;
            StopUpgradePulse();
        }

        public void StopUpgradePulse()
        {
            if (upgradePulse != null) StopCoroutine(upgradePulse);
            upgradePulse = null;
            foreach (var surface in pulseSurfaces)
                if (surface.Renderer != null)
                    surface.Renderer.SetPropertyBlock(surface.Original.isEmpty ? null : surface.Original, surface.Index);
            pulseSurfaces.Clear();
            pulsing = false;
            UpdateHighlight();
        }

        private static FactoryZoneInteraction selectedZone;

        public event Action<FactoryZoneInteraction> ZoneSelected;
        public event Action<FactoryZoneInteraction> ZoneDeselected;

        private void Awake()
        {
            isSelected = highlightOnStart;

            UpdateHighlight();
        }

        private void OnEnable()
        {
            if (cameraController != null)
            {
                cameraController.UserStartedCameraMovement +=
                    OnUserStartedCameraMovement;
            }
        }

        private void OnDisable()
        {
            StopUpgradePulse();
            if (cameraController != null)
            {
                cameraController.UserStartedCameraMovement -=
                    OnUserStartedCameraMovement;
            }

            if (selectedZone == this)
            {
                selectedZone = null;
            }
        }

        public void OnPointerClick(
            PointerEventData eventData)
        {
            if (!eventData.eligibleForClick || eventData.button != PointerEventData.InputButton.Left || eventData.dragging ||
                GameplayCameraController.ExceedsDragThreshold(eventData.pressPosition, eventData.position) ||
                (cameraController != null && cameraController.SuppressZoneClick)) return;
            SelectZone();

            Debug.Log(
                $"Factory zone clicked: {GetZoneName()}",
                this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Camera owns movement; EventSystem only cancels the click on release.
            eventData.eligibleForClick = false;
            ClearSelection();
        }

        public void OnDrag(PointerEventData eventData) { }
        public void OnEndDrag(PointerEventData eventData) { eventData.eligibleForClick = false; }

        public void SelectZone()
        {
            if (selectedZone != null &&
                selectedZone != this)
            {
                selectedZone.ClearSelection();
            }

            selectedZone = this;

            SetSelected(true);

            if (cameraController != null &&
                zoneArea != null)
            {
                cameraController.MoveToZone(
                    zoneArea);
            }

            ZoneSelected?.Invoke(this);
        }

        public void SetSelected(
            bool selected)
        {
            isSelected = selected;

            UpdateHighlight();
        }

        public void ClearSelection()
        {
            if (!isSelected)
            {
                return;
            }

            SetSelected(false);

            if (selectedZone == this)
            {
                selectedZone = null;
            }

            ZoneDeselected?.Invoke(this);
        }

        private void OnUserStartedCameraMovement()
        {
            if (!isSelected)
            {
                return;
            }

            ClearSelection();
        }

        private void UpdateHighlight()
        {
            if (zoneHighlight != null)
            {
                zoneHighlight.SetActive(isSelected || pulsing);
            }

            if (zoneBorder != null)
            {
                zoneBorder.SetActive(isSelected || pulsing);
            }
        }

        private string GetZoneName()
        {
            if (transform.parent != null)
            {
                return transform.parent.name;
            }

            return gameObject.name;
        }

        public bool IsSelected
        {
            get
            {
                return isSelected;
            }
        }

        public static FactoryZoneInteraction SelectedZone
        {
            get
            {
                return selectedZone;
            }
        }
    }
}
