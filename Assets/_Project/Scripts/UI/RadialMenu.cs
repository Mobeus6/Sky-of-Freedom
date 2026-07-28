using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class RadialMenu : MonoBehaviour
{
    [SerializeField] private RectTransform mainButton;
    [SerializeField] private RectTransform[] menuButtons;

    [Header("Animation")]
    [SerializeField] private float moveDuration = 0.35f;
    [SerializeField] private float stagger = 0.05f;
    [SerializeField] private Ease moveEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;

    private Vector2[] targetPositions;
    private bool isOpen;

    private void Awake()
    {
        targetPositions = new Vector2[menuButtons.Length];

        for (int i = 0; i < menuButtons.Length; i++)
        {
            targetPositions[i] = menuButtons[i].anchoredPosition;

            menuButtons[i].anchoredPosition = mainButton.anchoredPosition;
            menuButtons[i].localScale = Vector3.zero;
            menuButtons[i].gameObject.SetActive(false);
        }
    }

    public void ToggleMenu()
    {
        if (isOpen)
            Close();
        else
            Open();

        isOpen = !isOpen;
    }

    private void Open()
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            RectTransform button = menuButtons[i];

            button.gameObject.SetActive(true);
            button.anchoredPosition = mainButton.anchoredPosition;
            button.localScale = Vector3.zero;

            DG.Tweening.Sequence seq = DOTween.Sequence();

            seq.AppendInterval(i * stagger);

            seq.Append(
                button.DOAnchorPos(targetPositions[i], moveDuration)
                      .SetEase(moveEase));

            seq.Join(
                button.DOScale(1f, moveDuration));
        }
    }

    private void Close()
    {
        for (int i = menuButtons.Length - 1; i >= 0; i--)
        {
            RectTransform button = menuButtons[i];

            DG.Tweening.Sequence seq = DOTween.Sequence();

            seq.AppendInterval((menuButtons.Length - 1 - i) * stagger);

            seq.Append(
                button.DOAnchorPos(mainButton.anchoredPosition, moveDuration)
                      .SetEase(hideEase));

            seq.Join(
                button.DOScale(0f, moveDuration));

            seq.OnComplete(() =>
            {
                button.gameObject.SetActive(false);
            });
        }
    }
}