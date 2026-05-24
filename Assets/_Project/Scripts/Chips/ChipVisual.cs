using System;
using UnityEngine;

public class ChipVisual : MonoBehaviour
{
    [Serializable]
    private class ChipMaterialEntry
    {
        public ChipDenomination denomination;
        public Material material;
    }

    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private ChipMaterialEntry[] chipMaterials;

    public void Setup(ChipDenomination denomination)
    {
        SetMaterial(denomination);
        DisableColliders();
    }

    public void ResetVisual()
    {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    private void SetMaterial(ChipDenomination denomination)
    {
        Material material = GetMaterial(denomination);

        if (material == null)
            return;

        Renderer renderer = targetRenderer != null ? targetRenderer : GetComponentInChildren<Renderer>();

        if (renderer != null)
            renderer.sharedMaterial = material;
    }

    private Material GetMaterial(ChipDenomination denomination)
    {
        foreach (ChipMaterialEntry entry in chipMaterials)
        {
            if (entry != null && entry.denomination == denomination)
                return entry.material;
        }

        return null;
    }

    private void DisableColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);

        foreach (Collider collider in colliders)
            collider.enabled = false;
    }
}