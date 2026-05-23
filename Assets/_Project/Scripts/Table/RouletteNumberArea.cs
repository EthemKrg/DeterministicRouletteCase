using UnityEngine;

public class RouletteNumberArea : MonoBehaviour
{
    [SerializeField] private string slotId;
    [SerializeField] private Renderer targetRenderer;

    private Material normalMaterial;

    public string SlotId => slotId;

    private void Awake()
    {
        CacheRenderer();
        CacheNormalMaterial();
    }

    public void SetSlotId(string value)
    {
        slotId = value;
    }

    public void SetHighlight(Material highlightMaterial)
    {
        if (highlightMaterial == null)
            return;

        CacheRenderer();

        if (targetRenderer == null)
            return;

        targetRenderer.sharedMaterial = highlightMaterial;
    }

    public void ClearHighlight()
    {
        CacheRenderer();

        if (targetRenderer == null || normalMaterial == null)
            return;

        targetRenderer.sharedMaterial = normalMaterial;
    }

    private void CacheRenderer()
    {
        if (targetRenderer != null)
            return;

        targetRenderer = GetComponent<Renderer>();
    }

    private void CacheNormalMaterial()
    {
        if (targetRenderer == null)
            return;

        normalMaterial = targetRenderer.sharedMaterial;
    }
}