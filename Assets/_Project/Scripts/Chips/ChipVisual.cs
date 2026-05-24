using System;
using TMPro;
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
    [SerializeField] private TMP_Text valueLabel;
    [SerializeField] private ChipMaterialEntry[] chipMaterials;

    public void Setup(ChipDenomination denomination)
    {
        SetLabel(denomination);
        SetMaterial(denomination);
        DisableColliders();
    }

    public void ResetVisual()
    {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    private void SetLabel(ChipDenomination denomination)
    {
        if (valueLabel == null)
            return;

        int value = (int)denomination;
        valueLabel.text = value >= 1000 ? $"{value / 1000}K" : value.ToString();
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