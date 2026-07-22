using UnityEngine;

public class WallTransparency : MonoBehaviour
{
    [Header("Transparency")]
    [SerializeField, Range(0f, 1f)]
    private float transparentAlpha = 0.25f;

    [SerializeField]
    private float fadeSpeed = 5f;

    private float currentAlpha = 1f;
    private float targetAlpha = 1f;

    private Material material;
    private Color originalColor;

    private bool isTransparentTarget;

    private static readonly int BaseColorProperty =
        Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        Renderer renderer = GetComponent<Renderer>();

        if (renderer == null)
        {
            Debug.LogError(
                $"WallTransparency: Renderer not found on {gameObject.name}"
            );

            enabled = false;
            return;
        }

        // Створюємо унікальний Material Instance
        material = renderer.material;

        originalColor =
            material.GetColor(BaseColorProperty);

        currentAlpha = originalColor.a;
        targetAlpha = currentAlpha;

        ApplyAlpha(currentAlpha);
    }

    private void Update()
    {
        currentAlpha = Mathf.Lerp(
            currentAlpha,
            targetAlpha,
            fadeSpeed * Time.deltaTime
        );

        ApplyAlpha(currentAlpha);
    }

    public void SetTransparent(
        float alpha,
        float speed)
    {
        transparentAlpha = alpha;
        fadeSpeed = speed;

        targetAlpha = transparentAlpha;
        isTransparentTarget = true;
    }

    public void SetOpaque(float speed)
    {
        fadeSpeed = speed;

        targetAlpha = 1f;
        isTransparentTarget = false;
    }

    private void ApplyAlpha(float alpha)
    {
        if (material == null)
            return;

        Color color = originalColor;

        color.a = alpha;

        material.SetColor(
            BaseColorProperty,
            color
        );
    }
}