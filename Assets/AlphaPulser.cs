using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class AlphaPulser : MonoBehaviour
{
    [Header("Renderer (optional)")]
    public Renderer targetRenderer;
    public int materialIndex = 0;

    [Header("Pulse Settings")]
    [Range(0f, 1f)] public float minAlpha = 0.2f;
    [Range(0f, 1f)] public float maxAlpha = 1f;
    public float pulseSpeed = 1f;

    private MaterialPropertyBlock mpb;
    private Color baseColor;

    private void Awake()
    {
        if (!targetRenderer)
            targetRenderer = GetComponent<Renderer>();

        if (!targetRenderer)
        {
            Debug.LogError("[URPAlphaPulser] No renderer found.");
            enabled = false;
            return;
        }

        // Ensure valid index
        materialIndex = Mathf.Clamp(materialIndex, 0, targetRenderer.sharedMaterials.Length - 1);

        // Cache the original color
        baseColor = targetRenderer.sharedMaterials[materialIndex].GetColor("_BaseColor");

        mpb = new MaterialPropertyBlock();
    }

    private void Update()
    {
        // Pulsating 0→1→0 timeline
        float t = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

        // Apply the alpha to the BaseColor (URP uses _BaseColor)
        Color c = baseColor;
        c.a = alpha;

        mpb.SetColor("_BaseColor", c);
        targetRenderer.SetPropertyBlock(mpb, materialIndex);
    }
}