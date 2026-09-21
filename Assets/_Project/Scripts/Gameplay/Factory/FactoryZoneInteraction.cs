using System;
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
                zoneHighlight.SetActive(isSelected);
            }

            if (zoneBorder != null)
            {
                zoneBorder.SetActive(isSelected);
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
