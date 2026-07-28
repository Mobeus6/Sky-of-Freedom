using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class WallTransparency : MonoBehaviour
{
    [Header("Transparency")]
    [SerializeField, Range(0f, 1f)]
    private float transparentOpacity = 0.25f;

    [SerializeField]
    private float fadeSpeed = 5f;

    private float currentOpacity = 1f;
    private float targetOpacity = 1f;

    private Renderer wallRenderer;
    private MaterialPropertyBlock propertyBlock;

    private static readonly int OpacityProperty =
        Shader.PropertyToID("_Opacity");

    private void Awake()
    {
        wallRenderer = GetComponent<Renderer>();

        propertyBlock = new MaterialPropertyBlock();

        currentOpacity = 1f;
        targetOpacity = 1f;

        ApplyOpacity(currentOpacity);
    }

    private void Update()
    {
        if (Mathf.Approximately(currentOpacity, targetOpacity))
            return;

        currentOpacity = Mathf.MoveTowards(
            currentOpacity,
            targetOpacity,
            fadeSpeed * Time.deltaTime);

        ApplyOpacity(currentOpacity);
    }

    private void ApplyOpacity(float opacity)
    {
        wallRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetFloat(
            OpacityProperty,
            opacity);

        wallRenderer.SetPropertyBlock(propertyBlock);
    }

    public void SetTransparent(
        float opacity,
        float speed)
    {
        transparentOpacity = opacity;
        fadeSpeed = speed;

        targetOpacity = transparentOpacity;
    }

    public void SetOpaque(float speed)
    {
        fadeSpeed = speed;

        targetOpacity = 1f;
    }
}